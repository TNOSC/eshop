// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Ordering;

/// <summary>
/// The handler delegates to the workflow, and does nothing else. That "nothing else" is the assertion
/// worth making — it is what keeps the god-handler the design doc warns about from creeping back.
/// </summary>
public sealed class PlaceOrderCommandHandlerTests
{
    private readonly IPlaceOrderWorkflow _workflow = Substitute.For<IPlaceOrderWorkflow>();
    private readonly Guid _customerId = Guid.CreateVersion7();

    [Fact]
    public async Task HandleAsync_Should_PassTheCommandStraightToTheWorkflow()
    {
        // Arrange
        var command = new PlaceOrderCommand(CustomerId: _customerId);
        OrderId orderId = OrderTestFactory.Pending(customerId: _customerId).Id;
        _workflow.ExecuteAsync(command: Arg.Any<PlaceOrderCommand>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Result<OrderId>>(result: orderId));

        // Act
        Result<OrderId> result = await HandleAsync(command: command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: orderId);
        await _workflow.Received(requiredNumberOfCalls: 1).ExecuteAsync(
            command: command,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnTheWorkflowsFailure_Unchanged()
    {
        // Arrange
        Error workflowError = OrderErrors.BasketEmpty(customerId: _customerId);
        _workflow.ExecuteAsync(command: Arg.Any<PlaceOrderCommand>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Result<OrderId>>(result: workflowError));

        // Act
        Result<OrderId> result = await HandleAsync(command: new PlaceOrderCommand(CustomerId: _customerId));

        // Assert
        // No reinterpretation: same type, same code, same wording as the workflow produced.
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: workflowError.Type);
        result.FirstError.Code.ShouldBe(expected: workflowError.Code);
        result.FirstError.Description.ShouldBe(expected: workflowError.Description);
    }

    [Fact]
    public void Handler_Should_TakeTheWorkflow_AsItsOnlyDependency()
    {
        // Act
        ParameterInfo[] parameters = typeof(PlaceOrderCommandHandler)
            .GetConstructors(bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single()
            .GetParameters();

        // Assert
        // The mechanical form of "no god handler": the day someone injects a repository or a port here
        // instead of adding a step, this fails.
        parameters.Length.ShouldBe(
            expected: 1,
            customMessage: $"PlaceOrderCommandHandler must depend on the workflow alone, but takes: {string.Join(separator: ", ", values: parameters.Select(selector: p => p.ParameterType.Name))}");
        parameters[0].ParameterType.ShouldBe(expected: typeof(IPlaceOrderWorkflow));
    }

    [Fact]
    public void Handler_Should_BeMarkedIdempotent()
    {
        // Assert
        // Placing an order is a create over a network, so a dropped connection must be retryable
        // without charging the customer twice. The effect-happens-once half is proved against a real
        // database in PlaceOrderOutboxTests; this only catches the attribute being dropped.
        typeof(PlaceOrderCommandHandler).GetCustomAttribute<IdempotentAttribute>()
            .ShouldNotBeNull(customMessage: "PlaceOrderCommandHandler must stay [Idempotent] — the Idempotency-Key header is part of the endpoint's contract");
    }

    private ValueTask<Result<OrderId>> HandleAsync(PlaceOrderCommand command) =>
        new PlaceOrderCommandHandler(workflow: _workflow)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
