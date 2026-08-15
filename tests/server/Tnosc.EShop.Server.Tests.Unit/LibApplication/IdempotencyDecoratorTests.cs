// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Application.Decorators;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.LibApplication;

/// <summary>
/// The command half of <see cref="IdempotencyDecorator"/> against a substituted store: who runs,
/// what comes back, and — because the guarantee is the transaction, not the table — whether the
/// transaction is committed or rolled back in each case.
/// </summary>
/// <remarks>
/// Each scenario that needs a different attribute or inner handler uses a different command type on
/// purpose. The decorator memoises both the <c>[Idempotent]</c> lookup and the handler name against
/// its own closed generic type, which in production identifies the inner handler uniquely because
/// DI registers exactly one handler per closed command interface. Reusing one command type with two
/// different inner handlers would collide on that cache in a way production never can.
/// </remarks>
public sealed class IdempotencyDecoratorTests
{
    private const string Key = "key-7f3c";

    private readonly IIdempotencyStore _store = Substitute.For<IIdempotencyStore>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GuardedHandler _inner = new();

    [Fact]
    public async Task HandleAsync_Should_BypassTheStoreEntirely_When_TheHandlerIsNotIdempotent()
    {
        // Arrange
        IdempotencyKeyContext.Current = null;
        var decorator = new IdempotencyDecorator.CommandHandler<PlainCommand, string>(
            innerHandler: new PlainHandler(),
            store: _store,
            unitOfWork: _unitOfWork);

        // Act
        Result<string> result = await decorator.HandleAsync(command: new PlainCommand(), cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: "plain");
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnKeyMissing_When_NoKeyWasSupplied()
    {
        // Arrange
        IdempotencyKeyContext.Current = null;

        // Act
        Result<string> result = await HandleAsync();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Idempotency.KeyMissing");
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        _inner.Calls.ShouldBe(expected: 0);
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_RunTheHandlerAndCommit_When_TheKeyIsAcquired()
    {
        // Arrange
        Claim(returns: IdempotencyClaim<string>.Acquired());

        // Act
        Result<string> result = await HandleAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: "handled");
        _inner.Calls.ShouldBe(expected: 1);
        await _store.Received(requiredNumberOfCalls: 1).CompleteAsync(
            key: Key,
            handlerName: Arg.Any<string>(),
            response: "handled",
            cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.Received(requiredNumberOfCalls: 1).CommitTransactionAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReplayTheRecordedResponse_Without_RunningTheHandler()
    {
        // Arrange
        Claim(returns: IdempotencyClaim<string>.Replay(response: "recorded"));

        // Act
        Result<string> result = await HandleAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: "recorded", customMessage: "a duplicate must be answered from the record, not re-executed");
        _inner.Calls.ShouldBe(expected: 0);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).RollbackTransactionAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnKeyReuse_When_TheSameKeyCarriesADifferentPayload()
    {
        // Arrange
        Claim(returns: IdempotencyClaim<string>.PayloadMismatch());

        // Act
        Result<string> result = await HandleAsync();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Idempotency.KeyReuse");
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        _inner.Calls.ShouldBe(expected: 0);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnResponseTypeMismatch_When_TheRecordedResponseCannotBeReplayed()
    {
        // Arrange
        Claim(returns: IdempotencyClaim<string>.ResponseTypeMismatch());

        // Act
        Result<string> result = await HandleAsync();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Idempotency.ResponseTypeMismatch");
        _inner.Calls.ShouldBe(expected: 0);
    }

    [Fact]
    public async Task HandleAsync_Should_RollbackAndReleaseTheKey_When_TheHandlerReturnsAnError()
    {
        // Arrange
        Claim(returns: IdempotencyClaim<string>.Acquired());
        _inner.Behaviour = static () => ValueTask.FromResult<Result<string>>(Error.Conflict(code: "Test.Rejected", description: "no"));

        // Act
        Result<string> result = await HandleAsync();

        // Assert
        result.FirstError.Code.ShouldBe(expected: "Test.Rejected", customMessage: "the handler's own verdict must reach the caller unchanged");
        await _store.DidNotReceive().CompleteAsync(
            key: Arg.Any<string>(),
            handlerName: Arg.Any<string>(),
            response: Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.Received(requiredNumberOfCalls: 1).RollbackTransactionAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitTransactionAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_RollbackAndRethrow_When_TheHandlerThrows()
    {
        // Arrange
        Claim(returns: IdempotencyClaim<string>.Acquired());
        _inner.Behaviour = static () => throw new InvalidOperationException(message: "boom");

        // Act
        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(actual: async () => await HandleAsync());

        // Assert
        thrown.Message.ShouldBe(expected: "boom");
        await _unitOfWork.Received(requiredNumberOfCalls: 1).RollbackTransactionAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_JoinTheOuterTransaction_When_OneIsAlreadyActive()
    {
        // Arrange
        Claim(returns: IdempotencyClaim<string>.Acquired());
        _unitOfWork.HasActiveTransaction.Returns(returnThis: true);

        // Act
        Result<string> result = await HandleAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitTransactionAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().RollbackTransactionAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    private void Claim(IdempotencyClaim<string> returns)
    {
        IdempotencyKeyContext.Current = Key;

        _store.ClaimAsync<string>(
                key: Arg.Any<string>(),
                handlerName: Arg.Any<string>(),
                requestHash: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: returns);
    }

    private ValueTask<Result<string>> HandleAsync() =>
        new IdempotencyDecorator.CommandHandler<GuardedCommand, string>(
                innerHandler: _inner,
                store: _store,
                unitOfWork: _unitOfWork)
            .HandleAsync(command: new GuardedCommand(Payload: "p"), cancellationToken: CancellationToken.None);

    private sealed record GuardedCommand(string Payload) : ICommand<string>;

    private sealed record PlainCommand : ICommand<string>;

    [Idempotent]
    private sealed class GuardedHandler : ICommandHandler<GuardedCommand, string>
    {
        public int Calls { get; private set; }

        public Func<ValueTask<Result<string>>>? Behaviour { get; set; }

        public ValueTask<Result<string>> HandleAsync(GuardedCommand command, CancellationToken cancellationToken = default)
        {
            Calls++;

            return Behaviour is null ? ValueTask.FromResult<Result<string>>("handled") : Behaviour();
        }
    }

    private sealed class PlainHandler : ICommandHandler<PlainCommand, string>
    {
        public ValueTask<Result<string>> HandleAsync(PlainCommand command, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Result<string>>("plain");
    }
}
