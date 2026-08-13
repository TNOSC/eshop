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
using Tnosc.EShop.Server.Application.Identity.Commands.ProvisionCustomer;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Identity;

/// <summary>
/// The handler orchestrates and propagates: it builds value objects, delegates the decision to
/// <see cref="CustomerFactory"/>, commits, and carries the domain's verdict out without reinterpreting it.
/// </summary>
public sealed class ProvisionCustomerCommandHandlerTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProvisionCustomerCommandHandler _handler;

    public ProvisionCustomerCommandHandlerTests()
    {
        _repository
            .GetByExternalIdAsync(externalUserId: Arg.Any<ExternalUserId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: null));

        _repository
            .GetByEmailAsync(email: Arg.Any<Email>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: null));

        _handler = new ProvisionCustomerCommandHandler(repository: _repository, unitOfWork: _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_Should_ReportWasCreated_And_Commit_When_TheAccountIsNew()
    {
        // Act
        Result<ProvisionCustomerResult> result = await _handler.HandleAsync(command: Command());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.WasCreated.ShouldBeTrue();
        result.Value.CustomerId.ShouldNotBe(expected: Guid.Empty);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReportNotCreated_When_TheAccountAlreadyHasAProfile()
    {
        // Arrange
        Customer existing = await CustomerTestFactory.RegisterAsync(externalUserId: "sub-1", email: "sami@example.com");
        _repository
            .GetByExternalIdAsync(externalUserId: Arg.Any<ExternalUserId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: existing));

        // Act
        Result<ProvisionCustomerResult> result = await _handler.HandleAsync(
            command: Command(externalUserId: "sub-1", email: "sami@example.com"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.WasCreated.ShouldBeFalse();
        result.Value.CustomerId.ShouldBe(expected: existing.Id.Value);
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheConflict_Unchanged_When_TheEmailBelongsToAnotherAccount()
    {
        // Arrange
        Customer other = await CustomerTestFactory.RegisterAsync(externalUserId: "sub-other", email: "taken@example.com");
        _repository
            .GetByEmailAsync(email: Arg.Any<Email>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: other));

        // Act
        Result<ProvisionCustomerResult> result = await _handler.HandleAsync(command: Command(email: "taken@example.com"));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Customer.EmailAlreadyRegistered");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, "sami@example.com", "ExternalUserId.Empty")]
    [InlineData("sub-1", "not-an-email", "Email.InvalidFormat")]
    public async Task HandleAsync_Should_PropagateTheValueObjectError_And_NotCommit(
        string? externalUserId,
        string email,
        string expectedCode)
    {
        // Act
        Result<ProvisionCustomerResult> result = await _handler.HandleAsync(
            command: Command(externalUserId: externalUserId, email: email));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: expectedCode);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateThePhoneNumberError_When_ASuppliedNumberIsMalformed()
    {
        // Act
        Result<ProvisionCustomerResult> result = await _handler.HandleAsync(command: Command(phoneNumber: "12345"));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "PhoneNumber.InvalidFormat");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_When_NoPhoneNumberIsSupplied()
    {
        // Act
        Result<ProvisionCustomerResult> result = await _handler.HandleAsync(command: Command(phoneNumber: null));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    private static ProvisionCustomerCommand Command(
        string? externalUserId = "sub-1",
        string? email = "sami@example.com",
        string? phoneNumber = null) =>
        new(ExternalUserId: externalUserId,
            Email: email,
            FirstName: "Sami",
            LastName: "Shopper",
            PhoneNumber: phoneNumber);
}
