// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.Lib.Web.Results;
using Xunit;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Store.Assistant;

public sealed class ShoppingAssistantServiceTests
{
    private readonly IShoppingAssistantApi _assistantApi = Substitute.For<IShoppingAssistantApi>();
    private readonly ShoppingAssistantService _sut;

    private int _updateCount;

    public ShoppingAssistantServiceTests() => _sut = new ShoppingAssistantService(assistantApi: _assistantApi);

    [Fact]
    public async Task SendAsync_Should_RejectTheDraft_Without_CallingTheApi_When_ItIsEmpty()
    {
        // Arrange
        ShoppingAssistantViewModel viewModel = new() { Draft = "   " };

        // Act
        ClientResult result = await _sut.SendAsync(
            viewModel: viewModel,
            onUpdatedAsync: CountUpdateAsync,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        viewModel.Messages.ShouldBeEmpty();
        _assistantApi.DidNotReceiveWithAnyArgs().SendAsync(
            messages: default!,
            threadId: default!,
            cancellationToken: default);
    }

    [Fact]
    public async Task SendAsync_Should_ConcatenateEveryDeltaOntoOneReply_And_NotifyPerDelta()
    {
        // Arrange
        ShoppingAssistantViewModel viewModel = new() { Draft = "  what keyboards do you have?  " };
        StubStream(updates: [Text(text: "We stock "), Text(text: "three keyboards.")]);

        // Act
        ClientResult result = await _sut.SendAsync(
            viewModel: viewModel,
            onUpdatedAsync: CountUpdateAsync,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        viewModel.Messages.Count.ShouldBe(expected: 2);
        viewModel.Messages[0].IsFromShopper.ShouldBeTrue();
        viewModel.Messages[0].Text.ShouldBe(expected: "what keyboards do you have?");
        viewModel.Messages[1].IsFromShopper.ShouldBeFalse();
        viewModel.Messages[1].Text.ShouldBe(expected: "We stock three keyboards.");
        viewModel.Messages[1].IsLookingUp.ShouldBeFalse();
        viewModel.Draft.ShouldBeNull();

        // One before the stream opens, then one per delta.
        _updateCount.ShouldBe(expected: 4);
    }

    [Fact]
    public async Task SendAsync_Should_SendTheWholeHistoryAndTheSameThread_On_ASecondTurn()
    {
        // Arrange
        ShoppingAssistantViewModel viewModel = new() { Draft = "first" };
        StubStream(updates: [Text(text: "one")]);
        await _sut.SendAsync(viewModel: viewModel, onUpdatedAsync: CountUpdateAsync, cancellationToken: CancellationToken.None);

        viewModel.Draft = "second";
        StubStream(updates: [Text(text: "two")]);

        // Act
        await _sut.SendAsync(viewModel: viewModel, onUpdatedAsync: CountUpdateAsync, cancellationToken: CancellationToken.None);

        // Assert — AG-UI re-sends the full conversation on every turn, so the second call carries three.
        _assistantApi.Received(requiredNumberOfCalls: 1).SendAsync(
            messages: Arg.Is<IReadOnlyList<ChatMessage>>(predicate: messages =>
                messages.Count == 3
                && messages[0].Text == "first"
                && messages[1].Text == "one"
                && messages[2].Text == "second"),
            threadId: viewModel.ThreadId,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_Should_FlagTheReplyAsLookingUp_While_AToolCallIsInFlight()
    {
        // Arrange
        ShoppingAssistantViewModel viewModel = new() { Draft = "anything in stock?" };
        ChatResponseUpdate toolCall = new()
        {
            Role = ChatRole.Assistant,
            Contents = [new FunctionCallContent(callId: "1", name: "catalog_list_products")],
        };

        List<bool> lookingUpPerUpdate = [];
        StubStream(updates: [toolCall, Text(text: "Yes — three keyboards.")]);

        // Act
        await _sut.SendAsync(
            viewModel: viewModel,
            onUpdatedAsync: () =>
            {
                lookingUpPerUpdate.Add(item: viewModel.Messages[^1].IsLookingUp);
                return Task.CompletedTask;
            },
            cancellationToken: CancellationToken.None);

        // Assert — the tool call renders as "looking up", never as text.
        lookingUpPerUpdate.ShouldBe(expected: [true, true, false, false]);
        viewModel.Messages[^1].Text.ShouldBe(expected: "Yes — three keyboards.");
    }

    [Fact]
    public async Task SendAsync_Should_ReturnAFailure_When_TheAgentHostIsUnreachable()
    {
        // Arrange
        ShoppingAssistantViewModel viewModel = new() { Draft = "hello" };
        _assistantApi.SendAsync(
                messages: Arg.Any<IReadOnlyList<ChatMessage>>(),
                threadId: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Throws(ex: new HttpRequestException(message: "connection refused"));

        // Act
        ClientResult result = await _sut.SendAsync(
            viewModel: viewModel,
            onUpdatedAsync: CountUpdateAsync,
            cancellationToken: CancellationToken.None);

        // Assert — a transport failure is a reportable problem, never an escaping exception.
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.Title.ShouldBe(expected: ErrorCodeMessages.AssistantUnavailable);
        viewModel.Messages[^1].IsLookingUp.ShouldBeFalse();
    }

    [Fact]
    public async Task SendAsync_Should_SayItFoundNothing_When_TheRunProducedNoText()
    {
        // Arrange
        ShoppingAssistantViewModel viewModel = new() { Draft = "hello" };
        StubStream(updates: []);

        // Act
        ClientResult result = await _sut.SendAsync(
            viewModel: viewModel,
            onUpdatedAsync: CountUpdateAsync,
            cancellationToken: CancellationToken.None);

        // Assert — an empty bubble reads like a rendering bug, so the service fills one in.
        result.IsSuccess.ShouldBeTrue();
        viewModel.Messages[^1].Text.ShouldNotBeEmpty();
    }

    private static ChatResponseUpdate Text(string text) =>
        new(role: ChatRole.Assistant, content: text);

    private static async IAsyncEnumerable<ChatResponseUpdate> ToStreamAsync(
        IReadOnlyList<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (ChatResponseUpdate update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    private void StubStream(IReadOnlyList<ChatResponseUpdate> updates) =>
        _assistantApi.SendAsync(
                messages: Arg.Any<IReadOnlyList<ChatMessage>>(),
                threadId: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ToStreamAsync(updates: updates));

    private Task CountUpdateAsync()
    {
        _updateCount++;
        return Task.CompletedTask;
    }
}
