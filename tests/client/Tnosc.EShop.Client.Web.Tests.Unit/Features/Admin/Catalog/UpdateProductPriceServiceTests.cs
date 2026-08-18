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

public sealed class UpdateProductPriceServiceTests
{
    private readonly ICatalogApi _catalogApi = Substitute.For<ICatalogApi>();
    private readonly UpdateProductPriceService _sut;

    public UpdateProductPriceServiceTests() => _sut = new UpdateProductPriceService(catalogApi: _catalogApi);

    [Fact]
    public async Task SubmitAsync_Should_FailWithoutCallingTheApi_When_TheCurrencyIsMissing()
    {
        // Arrange
        UpdateProductPriceViewModel viewModel = new() { Amount = 10m, Currency = string.Empty };

        // Act
        ClientResult result = await _sut.SubmitAsync(productId: Guid.CreateVersion7(), viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.Errors!.ShouldContainKey(key: nameof(UpdateProductPriceViewModel.Currency));
        await _catalogApi.DidNotReceive().UpdateProductPriceAsync(
            productId: Arg.Any<Guid>(),
            request: Arg.Any<UpdateProductPriceRequest>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_Should_MapTheViewModelAndCallTheApi_When_TheViewModelIsValid()
    {
        // Arrange
        var productId = Guid.CreateVersion7();
        UpdateProductPriceViewModel viewModel = new() { Amount = 12.5m, Currency = "USD" };

        _catalogApi.UpdateProductPriceAsync(
                productId: Arg.Any<Guid>(),
                request: Arg.Any<UpdateProductPriceRequest>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult.Success()));

        // Act
        ClientResult result = await _sut.SubmitAsync(productId: productId, viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _catalogApi.Received(requiredNumberOfCalls: 1).UpdateProductPriceAsync(
            productId: productId,
            request: Arg.Is<UpdateProductPriceRequest>(predicate: r => r.Amount == 12.5m && r.Currency == "USD"),
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
