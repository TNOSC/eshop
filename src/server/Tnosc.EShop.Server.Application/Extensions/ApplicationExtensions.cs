// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Decorators;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Application.Extensions;
using Tnosc.Lib.Application.Validations;

namespace Tnosc.EShop.Server.Application.Extensions;

/// <summary>
/// Registers the application services
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// Registers the Application layer's command handlers, validators, domain event handlers
    /// wired with the standard command decorator pipeline.
    /// </summary>
    /// <param name="services"> The service collection to register the application services with.</param>
    /// <returns> The updated service collection. </returns>
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services.AddCommands()
                .AddDomainEvents();


    private static IServiceCollection AddCommands(this IServiceCollection services)
    {
        services.Scan(action: s => s.FromAssemblies(assemblies: AssemblyReference.Assembly)
            .AddClasses(action: c => c.AssignableTo(type: typeof(ICommandHandler<>)), publicOnly: false)
                .As(selector: implementationType => ScanExtensions.ClosedInterfacesOf(implementationType: implementationType, openGenericInterface: typeof(ICommandHandler<>))).WithScopedLifetime()
            .AddClasses(action: c => c.AssignableTo(type: typeof(ICommandHandler<,>)), publicOnly: false)
                .As(selector: implementationType => ScanExtensions.ClosedInterfacesOf(implementationType: implementationType, openGenericInterface: typeof(ICommandHandler<,>))).WithScopedLifetime()
            .AddClasses(action: c => c.AssignableTo(type: typeof(IValidator<>)), publicOnly: false)
                .As(selector: implementationType => ScanExtensions.ClosedInterfacesOf(implementationType: implementationType, openGenericInterface: typeof(IValidator<>))).WithScopedLifetime()
            .AddClasses(action: c => c.AssignableTo(type: typeof(IDomainEventHandler<>)), publicOnly: false)
                .As(selector: implementationType => ScanExtensions.ClosedInterfacesOf(implementationType: implementationType, openGenericInterface: typeof(IDomainEventHandler<>))).WithScopedLifetime());

        services.TryDecorate(serviceType: typeof(ICommandHandler<,>), decoratorType: typeof(IdempotencyDecorator.CommandHandler<,>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<,>), decoratorType: typeof(TransactionDecorator.CommandHandler<,>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<,>), decoratorType: typeof(CacheInvalidationDecorator.CommandHandler<,>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<,>), decoratorType: typeof(RetryDecorator.CommandHandler<,>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<,>), decoratorType: typeof(ValidationDecorator.CommandHandler<,>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<,>), decoratorType: typeof(ExceptionDecorator.CommandHandler<,>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<,>), decoratorType: typeof(LoggingDecorator.CommandHandler<,>));

        // Commands (no response) — identical sequence using the *.CommandBaseHandler<> variants.
        services.TryDecorate(serviceType: typeof(ICommandHandler<>), decoratorType: typeof(IdempotencyDecorator.CommandBaseHandler<>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<>), decoratorType: typeof(TransactionDecorator.CommandBaseHandler<>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<>), decoratorType: typeof(CacheInvalidationDecorator.CommandBaseHandler<>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<>), decoratorType: typeof(RetryDecorator.CommandBaseHandler<>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<>), decoratorType: typeof(ValidationDecorator.CommandBaseHandler<>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<>), decoratorType: typeof(ExceptionDecorator.CommandBaseHandler<>));
        services.TryDecorate(serviceType: typeof(ICommandHandler<>), decoratorType: typeof(LoggingDecorator.CommandBaseHandler<>));

        return services;
    }

    private static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        services.TryDecorate(serviceType: typeof(IDomainEventHandler<>), decoratorType: typeof(IdempotencyDecorator.DomainEventHandler<>));
        services.TryDecorate(serviceType: typeof(IDomainEventHandler<>), decoratorType: typeof(RetryDecorator.DomainEventHandler<>));

        return services;
    }
}
