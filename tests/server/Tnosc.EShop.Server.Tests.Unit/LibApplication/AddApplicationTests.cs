// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Tests.Unit.LibApplication.Fakes;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Decorators;
using Tnosc.Lib.Application.Extensions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.LibApplication;

public sealed class AddApplicationTests
{
    private static readonly Assembly TestAssembly = typeof(FakeCommandHandler).Assembly;

    [Fact]
    public void AddApplication_Should_Not_Throw_When_NoHandlersAreRegistered()
    {
        ServiceCollection services = new();

        Action act = () => services.AddApplication(typeof(object).Assembly);

        act.ShouldNotThrow();
    }

    [Fact]
    public async Task AddApplication_Should_ProduceCommandDecoratorOrder_Matching_LoggingExceptionValidationRetryCacheInvalidationTransactionHandler()
    {
        ServiceProvider provider = BuildProvider();

        ICommandHandler<FakeCommand, string> handler = provider.GetRequiredService<ICommandHandler<FakeCommand, string>>();

        List<string> chain = UnwrapDecoratorChain(handler);

        chain.ShouldBe([
            nameof(LoggingDecorator),
            nameof(ExceptionDecorator),
            nameof(ValidationDecorator),
            nameof(RetryDecorator),
            nameof(CacheInvalidationDecorator),
            nameof(TransactionDecorator),
            nameof(FakeCommandHandler),
        ]);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddApplication_Should_ProduceQueryDecoratorOrder_Matching_LoggingExceptionCacheableRetryHandler()
    {
        ServiceProvider provider = BuildProvider();

        IQueryHandler<FakeQuery, int> handler = provider.GetRequiredService<IQueryHandler<FakeQuery, int>>();

        List<string> chain = UnwrapDecoratorChain(handler);

        chain.ShouldBe([
            nameof(LoggingDecorator),
            nameof(ExceptionDecorator),
            nameof(CacheableDecorator),
            nameof(RetryDecorator),
            nameof(FakeQueryHandler),
        ]);

        await Task.CompletedTask;
    }

    /// <summary>
    /// The B2 regression: <c>[Retry(5)]</c>, <c>[CacheTag("catalog")]</c> and <c>[Transactional]</c>
    /// all live on <see cref="FakeCommandHandler"/>, which sits six layers deep once the full
    /// command pipeline wraps it. Before the fix, every decorator except the innermost one read
    /// attributes off its immediate neighbour's type and found nothing, so retries fell back to
    /// the hard-coded default of three and cache invalidation never ran. The handler fails its
    /// first three attempts, so only a correctly-discovered <c>MaxRetries = 5</c> lets it succeed.
    /// </summary>
    [Fact]
    public async Task AddApplication_Should_DiscoverAttributes_ThroughSixLayersOfDecoratorWrapping()
    {
        HybridCache cache = Substitute.For<HybridCache>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        ServiceProvider provider = BuildProvider(services =>
        {
            services.AddSingleton(cache);
            services.AddSingleton(unitOfWork);
        });

        ICommandHandler<FakeCommand, string> handler = provider.GetRequiredService<ICommandHandler<FakeCommand, string>>();

        Result<string> result = await handler.HandleAsync(new FakeCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("ok");

        // Transaction sits inside Retry, so each of the four attempts (three failures, one
        // success) opens its own transaction: three rollbacks, then one commit on success.
        await unitOfWork.Received(4).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await unitOfWork.Received(3).RollbackTransactionAsync(Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync("catalog", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The query-side counterpart of the B2 regression: <c>CacheableDecorator</c> wraps
    /// <c>RetryDecorator</c>, which wraps the real handler, so <c>[Cacheable]</c> sits one layer
    /// below where the pre-fix code looked. Calling the handler twice with the same cache key
    /// should invoke the real handler only once.
    /// </summary>
    [Fact]
    public async Task AddApplication_Should_DiscoverCacheableAttribute_ThroughRetryDecoratorWrapping()
    {
        ServiceProvider provider = BuildProvider(services => services.AddHybridCache());

        IQueryHandler<FakeQuery, int> handler = provider.GetRequiredService<IQueryHandler<FakeQuery, int>>();
        FakeQuery query = new(Key: 1);

        Result<int> first = await handler.HandleAsync(query, CancellationToken.None);
        Result<int> second = await handler.HandleAsync(query, CancellationToken.None);

        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        second.Value.ShouldBe(first.Value);
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
    {
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new CallCounter());
        services.AddSingleton(Substitute.For<IUnitOfWork>());
        services.AddSingleton(Substitute.For<HybridCache>());

        configure?.Invoke(services);

        services.AddApplication(TestAssembly);

        return services.BuildServiceProvider();
    }

    private static List<string> UnwrapDecoratorChain(object handler)
    {
        List<string> chain = [];
        object current = handler;

        while (true)
        {
            chain.Add(current.GetType().DeclaringType?.Name ?? current.GetType().Name);

            if (current is not IHandlerDecorator decorator)
            {
                break;
            }

            current = decorator.InnerHandler;
        }

        return chain;
    }
}
