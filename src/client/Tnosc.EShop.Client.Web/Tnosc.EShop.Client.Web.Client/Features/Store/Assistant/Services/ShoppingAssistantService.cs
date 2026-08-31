// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Infrastructure;
using Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.ViewModels;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Validation;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Services;

/// <summary>
/// The only type outside <see cref="IShoppingAssistantApi"/> that touches the assistant conversation.
/// </summary>
/// <remarks>
/// No <see cref="ChatMessage"/> or <see cref="ChatResponseUpdate"/> leaves this class. The AI
/// framework's vocabulary is a wire shape like any DTO, and the panel is written against view models
/// for the same reason every other component here is.
/// </remarks>
internal sealed class ShoppingAssistantService(IShoppingAssistantApi assistantApi, IBasketApi basketApi)
    : IShoppingAssistantService
{
    /// <summary>Adds one unit of a product to the shopper's basket, as chosen from a chat card.</summary>
    public async Task<ClientResult<int>> AddToBasketAsync(Guid productId, CancellationToken cancellationToken)
    {
        ClientResult<BasketDto> result = await basketApi.AddItemAsync(
            request: new AddItemToBasketRequest(ProductId: productId, Quantity: 1),
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<int>.Failure(problem: result.Problem!);
        }

        return ClientResult<int>.Success(value: result.Value.Items.Count);
    }

    public async Task<ClientResult> SendAsync(
        ShoppingAssistantViewModel viewModel,
        Func<Task> onUpdatedAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(argument: viewModel);
        ArgumentNullException.ThrowIfNull(argument: onUpdatedAsync);

        ClientProblem? validationProblem = ClientValidation.Validate(viewModel: viewModel);
        if (validationProblem is not null)
        {
            return ClientResult.Failure(problem: validationProblem);
        }

        string question = viewModel.Draft!.Trim();

        // The history is captured before the reply's placeholder is appended, or the assistant would
        // be sent an empty turn of its own alongside the question.
        List<ChatMessage> history = ToChatMessages(viewModel: viewModel, question: question);

        viewModel.Messages.Add(item: new ChatMessageViewModel { IsFromShopper = true, Text = question });

        ChatMessageViewModel reply = new() { IsFromShopper = false, IsLookingUp = true };
        viewModel.Messages.Add(item: reply);

        viewModel.Draft = null;
        await onUpdatedAsync.Invoke();

        return await StreamReplyAsync(
            history: history,
            threadId: viewModel.ThreadId,
            reply: reply,
            onUpdatedAsync: onUpdatedAsync,
            cancellationToken: cancellationToken);
    }

    /// <summary>Reads the reply's stream into <paramref name="reply"/>, one delta at a time.</summary>
    private async Task<ClientResult> StreamReplyAsync(
        List<ChatMessage> history,
        string threadId,
        ChatMessageViewModel reply,
        Func<Task> onUpdatedAsync,
        CancellationToken cancellationToken)
    {
        StringBuilder answer = new();

        try
        {
            IAsyncEnumerable<ChatResponseUpdate> updates = assistantApi.SendAsync(
                messages: history,
                threadId: threadId,
                cancellationToken: cancellationToken);

            await foreach (ChatResponseUpdate update in updates.WithCancellation(cancellationToken: cancellationToken))
            {
                // A tool call produces no text, and the model goes quiet for as long as the catalogue
                // lookup takes. Saying so is the difference between "thinking" and "broken".
                reply.IsLookingUp = update.Contents.Any(predicate: static content => content is FunctionCallContent);

                foreach (FunctionCallContent call in update.Contents.OfType<FunctionCallContent>())
                {
                    if (string.Equals(a: call.Name, b: AssistantToolNames.RenderProductCard, comparisonType: StringComparison.Ordinal))
                    {
                        reply.Products = ParseProductCards(arguments: call.Arguments);
                    }
                }

                if (!string.IsNullOrEmpty(value: update.Text))
                {
                    reply.IsLookingUp = false;
                    answer.Append(value: update.Text);
                    reply.Text = answer.ToString();
                }

                await onUpdatedAsync.Invoke();
            }
        }
        catch (HttpRequestException exception)
        {
            return Interrupted(reply: reply, errorCode: ErrorCodeMessages.AssistantUnavailable, detail: exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The stream itself timed out or was dropped downstream. A caller-requested cancellation
            // is a different thing entirely and is left to propagate.
            return Interrupted(reply: reply, errorCode: ErrorCodeMessages.AssistantInterrupted, detail: null);
        }

        reply.IsLookingUp = false;

        // A run that produced only tool traffic answers with nothing at all. Saying so beats leaving
        // an empty bubble that reads like a rendering bug.
        if (reply.Text.Length == 0)
        {
            reply.Text = "I could not find an answer to that.";
        }

        await onUpdatedAsync.Invoke();
        return ClientResult.Success();
    }

    private static ClientResult Interrupted(ChatMessageViewModel reply, string errorCode, string? detail)
    {
        reply.IsLookingUp = false;

        // Title carries the code as well: ErrorCodeMessages.Humanize looks a problem up by Title,
        // which is where the server's error code lands on a real problem document.
        return ClientResult.Failure(problem: new ClientProblem(
            Type: null,
            Title: errorCode,
            Status: null,
            Detail: detail,
            Instance: null,
            Errors: null,
            ErrorCode: errorCode,
            TraceId: null));
    }

    /// <summary>
    /// Maps the conversation so far, plus the new question, into the shape AG-UI expects.
    /// </summary>
    /// <remarks>
    /// The whole history goes on every turn — that is the protocol's design, not a missing
    /// optimisation. Turns whose text never arrived are skipped so a failed reply does not become an
    /// empty assistant message the model has to make sense of.
    /// </remarks>
    private static List<ChatMessage> ToChatMessages(ShoppingAssistantViewModel viewModel, string question)
    {
        List<ChatMessage> messages = [];

        foreach (ChatMessageViewModel message in viewModel.Messages)
        {
            if (message.Text.Length == 0)
            {
                continue;
            }

            messages.Add(item: new ChatMessage(
                role: message.IsFromShopper ? ChatRole.User : ChatRole.Assistant,
                content: message.Text));
        }

        messages.Add(item: new ChatMessage(role: ChatRole.User, content: question));
        return messages;
    }

    /// <summary>
    /// Reconstructs a <c>render_product_card</c> call's arguments into display view models. The
    /// dictionary is round-tripped through JSON rather than walked by hand, so the tool's declared
    /// schema and this parsing can never drift out of shape with one another.
    /// </summary>
    private static IReadOnlyList<ProductSummaryViewModel> ParseProductCards(IDictionary<string, object?>? arguments)
    {
        if (arguments is null)
        {
            return [];
        }

        string json = JsonSerializer.Serialize(value: arguments, options: AIJsonUtilities.DefaultOptions);
        RenderProductCardArgs? parsed = JsonSerializer.Deserialize<RenderProductCardArgs>(json: json, options: AIJsonUtilities.DefaultOptions);

        if (parsed is null)
        {
            return [];
        }

        return [.. parsed.Products.Select(selector: static product => new ProductSummaryViewModel
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            PriceAmount = product.PriceAmount,
            PriceCurrency = product.PriceCurrency,
            StockQuantity = product.StockQuantity,
        })];
    }
}
