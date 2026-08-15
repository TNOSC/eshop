// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Provides idempotency decorators for command handlers and domain event handlers marked
/// <see cref="IdempotentAttribute"/>, so their effects happen at most once per key however many
/// times the message arrives.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why one transaction.</b> The claim record and the handler's own writes commit or roll back
/// together, which is the whole guarantee: a crash can never leave a key burned with no effect, nor
/// an effect with no key. These decorators are therefore registered <b>innermost</b> — inside
/// <see cref="TransactionDecorator"/>, directly around the real handler. When
/// <see cref="TransactionalAttribute"/> already opened a transaction they join it and leave the
/// commit decision to the outer decorator; otherwise they own the transaction themselves.
/// </para>
/// <para>
/// <b>Why there is no "in progress" state.</b> A concurrent duplicate's claim insert blocks on the
/// first transaction's uncommitted row rather than seeing it, so an intermediate state is never
/// observable and never needs recording. When the first transaction commits, the second sees the
/// conflict and replays; when it rolls back, the second acquires the key and runs for real. The
/// cost is that duplicates block for the handler's duration instead of failing fast — which is
/// exactly what makes the replayed answer correct.
/// </para>
/// <para>
/// <b>Why a failed command releases its key.</b> A handler that returns an error <see cref="Result"/>
/// or throws rolls the transaction back, discarding the claim, so the caller may legitimately retry
/// the same key. Only a success burns it.
/// </para>
/// </remarks>
public static class IdempotencyDecorator
{
    private static readonly ConcurrentDictionary<Type, bool> IdempotentCache = new();
    private static readonly ConcurrentDictionary<Type, string> HandlerNameCache = new();

    private static readonly JsonSerializerOptions HashSerializerOptions = new() { WriteIndented = false };

    /// <summary>
    /// Idempotency decorator for command handlers that return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    /// <param name="store">The store recording which keys this handler has already answered.</param>
    /// <param name="unitOfWork">The unit of work whose transaction the claim must share.</param>
    public sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IIdempotencyStore store,
        IUnitOfWork unitOfWork)
        : ICommandHandler<TCommand, TResponse>, IHandlerDecorator
        where TCommand : ICommand<TResponse>
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the command at most once per idempotency key when the inner handler is marked
        /// <see cref="IdempotentAttribute"/>, replaying the recorded response for a duplicate.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            if (!IsIdempotent(handler: this, messageType: typeof(TCommand)))
            {
                return await innerHandler.HandleAsync(command: command, cancellationToken: cancellationToken);
            }

            string? key = IdempotencyKeyContext.Current;

            if (string.IsNullOrWhiteSpace(value: key))
            {
                return IdempotencyErrors.KeyMissing;
            }

            string handlerName = HandlerName(handler: this);
            string requestHash = ComputeRequestHash(request: command);
            bool ownsTransaction = await BeginAsync(unitOfWork: unitOfWork, cancellationToken: cancellationToken);

            try
            {
                IdempotencyClaim<TResponse> claim = await store.ClaimAsync<TResponse>(
                    key: key,
                    handlerName: handlerName,
                    requestHash: requestHash,
                    cancellationToken: cancellationToken);

                if (claim.Status is not IdempotencyClaimStatus.Acquired)
                {
                    // Nothing was written, so releasing the transaction is both correct and cheaper
                    // than committing it.
                    await RollbackAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);

                    return Answered(claim: claim);
                }

                Result<TResponse> result = await innerHandler.HandleAsync(command: command, cancellationToken: cancellationToken);

                if (result.IsError)
                {
                    await RollbackAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);

                    return result;
                }

                await store.CompleteAsync(
                    key: key,
                    handlerName: handlerName,
                    response: result.Value,
                    cancellationToken: cancellationToken);

                await CommitAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);

                return result;
            }
            catch
            {
                await RollbackAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);

                throw;
            }
        }

        /// <summary>
        /// Maps a claim the caller did not acquire onto the result it must return instead of
        /// running the handler.
        /// </summary>
        /// <param name="claim">The non-acquired claim.</param>
        private static Result<TResponse> Answered(IdempotencyClaim<TResponse> claim) =>
            claim.Status switch
            {
                IdempotencyClaimStatus.Replay => claim.Response!,
                IdempotencyClaimStatus.PayloadMismatch => IdempotencyErrors.KeyReuse,
                _ => IdempotencyErrors.ResponseTypeMismatch,
            };
    }

    /// <summary>
    /// Idempotency decorator for command handlers that do not return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    /// <param name="store">The store recording which keys this handler has already answered.</param>
    /// <param name="unitOfWork">The unit of work whose transaction the claim must share.</param>
    public sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        IIdempotencyStore store,
        IUnitOfWork unitOfWork)
        : ICommandHandler<TCommand>, IHandlerDecorator
        where TCommand : ICommand
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the command at most once per idempotency key when the inner handler is marked
        /// <see cref="IdempotentAttribute"/>, succeeding without re-running it for a duplicate.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            if (!IsIdempotent(handler: this, messageType: typeof(TCommand)))
            {
                return await innerHandler.HandleAsync(command: command, cancellationToken: cancellationToken);
            }

            string? key = IdempotencyKeyContext.Current;

            if (string.IsNullOrWhiteSpace(value: key))
            {
                return IdempotencyErrors.KeyMissing;
            }

            string handlerName = HandlerName(handler: this);
            string requestHash = ComputeRequestHash(request: command);
            bool ownsTransaction = await BeginAsync(unitOfWork: unitOfWork, cancellationToken: cancellationToken);

            try
            {
                IdempotencyClaimStatus status = await store.ClaimAsync(
                    key: key,
                    handlerName: handlerName,
                    requestHash: requestHash,
                    cancellationToken: cancellationToken);

                if (status is not IdempotencyClaimStatus.Acquired)
                {
                    await RollbackAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);

                    return Answered(status: status);
                }

                Result result = await innerHandler.HandleAsync(command: command, cancellationToken: cancellationToken);

                if (result.IsError)
                {
                    await RollbackAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);

                    return result;
                }

                await store.CompleteAsync(key: key, handlerName: handlerName, cancellationToken: cancellationToken);

                await CommitAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);

                return result;
            }
            catch
            {
                await RollbackAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);

                throw;
            }
        }

        /// <summary>
        /// Maps a claim the caller did not acquire onto the result it must return. A replay simply
        /// succeeds — this handler shape records no response to hand back.
        /// </summary>
        /// <param name="status">The non-acquired claim status.</param>
        private static Result Answered(IdempotencyClaimStatus status) =>
            status switch
            {
                IdempotencyClaimStatus.PayloadMismatch => IdempotencyErrors.KeyReuse,
                _ => Result.Success(),
            };
    }

    /// <summary>
    /// Inbox decorator for domain event handlers, turning the outbox's at-least-once delivery into
    /// an at-most-once effect for handlers marked <see cref="IdempotentAttribute"/>.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type.</typeparam>
    /// <param name="innerHandler">The inner domain event handler.</param>
    /// <param name="store">The inbox recording which events this handler has already processed.</param>
    /// <param name="unitOfWork">The unit of work whose transaction the claim must share.</param>
    public sealed class DomainEventHandler<TEvent>(
        IDomainEventHandler<TEvent> innerHandler,
        IInboxStore store,
        IUnitOfWork unitOfWork)
        : IDomainEventHandler<TEvent>, IHandlerDecorator
        where TEvent : IDomainEvent
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the domain event unless this handler already processed it, claiming
        /// <see cref="IDomainEvent.Id"/> in the same transaction as the handler's own writes.
        /// </summary>
        /// <param name="event">The domain event to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
        {
            if (!IsIdempotent(handler: this, messageType: typeof(TEvent)))
            {
                await innerHandler.HandleAsync(@event: @event, cancellationToken: cancellationToken);

                return;
            }

            string handlerName = HandlerName(handler: this);
            bool ownsTransaction = await BeginAsync(unitOfWork: unitOfWork, cancellationToken: cancellationToken);

            try
            {
                bool claimed = await store.TryClaimAsync(
                    eventId: @event.Id,
                    handlerName: handlerName,
                    cancellationToken: cancellationToken);

                if (!claimed)
                {
                    await RollbackAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);

                    return;
                }

                await innerHandler.HandleAsync(@event: @event, cancellationToken: cancellationToken);

                await CommitAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);
            }
            catch
            {
                await RollbackAsync(unitOfWork: unitOfWork, ownsTransaction: ownsTransaction, cancellationToken: cancellationToken);

                throw;
            }
        }
    }

    /// <summary>
    /// Determines whether the unwrapped handler type is marked <see cref="IdempotentAttribute"/>,
    /// caching the result per <b>unwrapped handler</b> type to avoid repeated attribute lookups.
    /// </summary>
    /// <remarks>
    /// Keyed on the unwrapped type, not the decorator's: several domain event handlers subscribe to
    /// one event and therefore share a closed decorator type, so keying on that would give every
    /// sibling the first handler's answer.
    /// </remarks>
    /// <param name="handler">The decorator instance wrapping the handler.</param>
    /// <param name="messageType">The command or event type the handler processes.</param>
    private static bool IsIdempotent(object handler, Type messageType) =>
        IdempotentCache.GetOrAdd(key: HandlerChain.Unwrap(handler: handler), valueFactory: _ => HandlerMetadata.Find<IdempotentAttribute>(handler: handler, messageType: messageType) is not null);

    /// <summary>
    /// Resolves the durable name a key is scoped to, so two handlers can never collide on one
    /// caller-supplied key — and, for an event, so a dead-lettered handler can be matched back to
    /// the inbox claim it never made.
    /// </summary>
    /// <param name="handler">The decorator instance wrapping the handler.</param>
    private static string HandlerName(object handler) =>
        HandlerNameCache.GetOrAdd(key: HandlerChain.Unwrap(handler: handler), valueFactory: static unwrapped => unwrapped.FullName ?? unwrapped.Name);

    /// <summary>
    /// Hashes the request payload so a key replayed with different content can be told apart from a
    /// genuine retry, which would otherwise be answered with a response to a question nobody asked.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request to hash.</param>
    /// <returns>The lowercase hexadecimal SHA-256 of the serialized request.</returns>
    private static string ComputeRequestHash<TRequest>(TRequest request)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes<TRequest>(value: request, options: HashSerializerOptions);

        return Convert.ToHexStringLower(inArray: SHA256.HashData(source: payload));
    }

    /// <summary>
    /// Opens a transaction unless an outer decorator already owns one.
    /// </summary>
    /// <param name="unitOfWork">The unit of work to begin the transaction on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> when this decorator opened the transaction and is therefore
    /// responsible for ending it; <see langword="false"/> when an outer decorator owns it.
    /// </returns>
    private static async ValueTask<bool> BeginAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        if (unitOfWork.HasActiveTransaction)
        {
            return false;
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        return true;
    }

    /// <summary>
    /// Commits the transaction when this decorator owns it, otherwise leaves the decision to the
    /// outer decorator that does.
    /// </summary>
    /// <param name="unitOfWork">The unit of work holding the transaction.</param>
    /// <param name="ownsTransaction">Whether this decorator opened the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private static async ValueTask CommitAsync(IUnitOfWork unitOfWork, bool ownsTransaction, CancellationToken cancellationToken)
    {
        if (ownsTransaction)
        {
            await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Rolls the transaction back when this decorator owns it, otherwise leaves the decision to the
    /// outer decorator that does — which sees the error <see cref="Result"/> or the exception and
    /// rolls back for the same reason.
    /// </summary>
    /// <param name="unitOfWork">The unit of work holding the transaction.</param>
    /// <param name="ownsTransaction">Whether this decorator opened the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private static async ValueTask RollbackAsync(IUnitOfWork unitOfWork, bool ownsTransaction, CancellationToken cancellationToken)
    {
        if (ownsTransaction)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken: cancellationToken);
        }
    }
}
