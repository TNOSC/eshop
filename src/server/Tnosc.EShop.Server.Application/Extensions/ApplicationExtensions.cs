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
        services.AddCommands();


    private static IServiceCollection AddCommands(this IServiceCollection services)
    {
        services.Scan(s => s.FromAssemblies(AssemblyReference.Assembly)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .As(implementationType => ScanExtensions.ClosedInterfacesOf(implementationType, typeof(ICommandHandler<>))).WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .As(implementationType => ScanExtensions.ClosedInterfacesOf(implementationType, typeof(ICommandHandler<,>))).WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IValidator<>)), publicOnly: false)
                .As(implementationType => ScanExtensions.ClosedInterfacesOf(implementationType, typeof(IValidator<>))).WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .As(implementationType => ScanExtensions.ClosedInterfacesOf(implementationType, typeof(IDomainEventHandler<>))).WithScopedLifetime());

        // Commands (with response) — innermost first, the last TryDecorate call becomes outermost.
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(TransactionDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(CacheInvalidationDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(RetryDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(ExceptionDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));

        // Commands (no response) — identical sequence using the *.CommandBaseHandler<> variants.
        services.TryDecorate(typeof(ICommandHandler<>), typeof(TransactionDecorator.CommandBaseHandler<>));
        services.TryDecorate(typeof(ICommandHandler<>), typeof(CacheInvalidationDecorator.CommandBaseHandler<>));
        services.TryDecorate(typeof(ICommandHandler<>), typeof(RetryDecorator.CommandBaseHandler<>));
        services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandBaseHandler<>));
        services.TryDecorate(typeof(ICommandHandler<>), typeof(ExceptionDecorator.CommandBaseHandler<>));
        services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));

        return services;
    }
}
