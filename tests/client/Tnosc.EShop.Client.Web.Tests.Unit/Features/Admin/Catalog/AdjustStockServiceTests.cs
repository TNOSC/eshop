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

public sealed class AdjustStockServiceTests
{
    private readonly ICatalogApi _catalogApi = Substitute.For<ICatalogApi>();
    private readonly AdjustStockService _sut;

    public AdjustStockServiceTests() => _sut = new AdjustStockService(catalogApi: _catalogApi);

    [Fact]
    public async Task SubmitAsync_Should_FailWithoutCallingTheApi_When_TheDeltaIsZero()
    {
        // Arrange
        AdjustStockViewModel viewModel = new() { Delta = 0 };

        // Act
        ClientResult result = await _sut.SubmitAsync(productId: Guid.CreateVersion7(), viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.Errors!.ShouldContainKey(key: nameof(AdjustStockViewModel.Delta));
        await _catalogApi.DidNotReceive().AdjustStockAsync(
            productId: Arg.Any<Guid>(),
            request: Arg.Any<AdjustStockRequest>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_Should_MapTheDeltaAndCallTheApi_When_TheDeltaIsNonZero()
    {
        // Arrange
        var productId = Guid.CreateVersion7();
        AdjustStockViewModel viewModel = new() { Delta = -3 };

        _catalogApi.AdjustStockAsync(
                productId: Arg.Any<Guid>(),
                request: Arg.Any<AdjustStockRequest>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult.Success()));

        // Act
        ClientResult result = await _sut.SubmitAsync(productId: productId, viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _catalogApi.Received(requiredNumberOfCalls: 1).AdjustStockAsync(
            productId: productId,
            request: Arg.Is<AdjustStockRequest>(predicate: r => r.Delta == -3),
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
