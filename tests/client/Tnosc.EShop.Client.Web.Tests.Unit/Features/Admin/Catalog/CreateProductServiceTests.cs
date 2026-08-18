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
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Admin.Catalog;

public sealed class CreateProductServiceTests
{
    private readonly ICatalogApi _catalogApi = Substitute.For<ICatalogApi>();
    private readonly CreateProductService _sut;

    public CreateProductServiceTests() => _sut = new CreateProductService(catalogApi: _catalogApi);

    private static CreateProductViewModel ValidViewModel() => new()
    {
        Sku = "SKU-1",
        Name = "Widget",
        Description = "A widget",
        PriceAmount = 9.99m,
        PriceCurrency = "USD",
        StockQuantity = 10,
        CategoryId = Guid.CreateVersion7(),
        BrandId = Guid.CreateVersion7().ToString(),
    };

    [Fact]
    public async Task SubmitAsync_Should_FailWithoutCallingTheApi_When_ARequiredFieldIsMissing()
    {
        // Arrange
        CreateProductViewModel viewModel = ValidViewModel();
        viewModel.Sku = string.Empty;

        // Act
        ClientResult<Guid> result = await _sut.SubmitAsync(
            viewModel: viewModel,
            idempotencyKey: Guid.CreateVersion7(),
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.Errors!.ShouldContainKey(key: nameof(CreateProductViewModel.Sku));
        await _catalogApi.DidNotReceive().CreateProductAsync(
            request: Arg.Any<CreateProductRequest>(),
            idempotencyKey: Arg.Any<Guid>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_Should_FailWithoutCallingTheApi_When_TheBrandIdIsNotAGuid()
    {
        // Arrange
        CreateProductViewModel viewModel = ValidViewModel();
        viewModel.BrandId = "not-a-guid";

        // Act
        ClientResult<Guid> result = await _sut.SubmitAsync(
            viewModel: viewModel,
            idempotencyKey: Guid.CreateVersion7(),
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.Errors!.ShouldContainKey(key: nameof(CreateProductViewModel.BrandId));
        await _catalogApi.DidNotReceive().CreateProductAsync(
            request: Arg.Any<CreateProductRequest>(),
            idempotencyKey: Arg.Any<Guid>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_Should_MapTheViewModelAndCallTheApi_When_TheViewModelIsValid()
    {
        // Arrange
        CreateProductViewModel viewModel = ValidViewModel();
        var idempotencyKey = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();

        _catalogApi.CreateProductAsync(
                request: Arg.Any<CreateProductRequest>(),
                idempotencyKey: Arg.Any<Guid>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<Guid>.Success(value: productId)));

        // Act
        ClientResult<Guid> result = await _sut.SubmitAsync(
            viewModel: viewModel,
            idempotencyKey: idempotencyKey,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: productId);
        await _catalogApi.Received(requiredNumberOfCalls: 1).CreateProductAsync(
            request: Arg.Is<CreateProductRequest>(predicate: request =>
                request.Sku == viewModel.Sku
                && request.Name == viewModel.Name
                && request.Description == viewModel.Description
                && request.PriceAmount == viewModel.PriceAmount
                && request.PriceCurrency == viewModel.PriceCurrency
                && request.StockQuantity == viewModel.StockQuantity
                && request.CategoryId == viewModel.CategoryId
                && request.BrandId == Guid.Parse(input: viewModel.BrandId)),
            idempotencyKey: idempotencyKey,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_Should_SendNullDescription_When_TheDescriptionIsBlank()
    {
        // Arrange
        CreateProductViewModel viewModel = ValidViewModel();
        viewModel.Description = "   ";

        _catalogApi.CreateProductAsync(
                request: Arg.Any<CreateProductRequest>(),
                idempotencyKey: Arg.Any<Guid>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<Guid>.Success(value: Guid.CreateVersion7())));

        // Act
        await _sut.SubmitAsync(
            viewModel: viewModel,
            idempotencyKey: Guid.CreateVersion7(),
            cancellationToken: CancellationToken.None);

        // Assert
        await _catalogApi.Received(requiredNumberOfCalls: 1).CreateProductAsync(
            request: Arg.Is<CreateProductRequest>(predicate: request => request.Description == null),
            idempotencyKey: Arg.Any<Guid>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
