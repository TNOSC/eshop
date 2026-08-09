// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Decorators;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Application.Validations;

namespace Tnosc.Lib.Application.Extensions;

/// <summary>
/// Registers the application layer's command/query handlers, validators, and domain-event
/// handlers found in the specified assemblies, then wraps every handler with the standard
/// decorator pipeline.
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// Scans <paramref name="assemblies"/> for command handlers, query handlers, validators,
    /// and domain-event handlers, registers them with a scoped lifetime, and decorates
    /// command and query handlers with the standard cross-cutting pipeline.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="assemblies">The assemblies to scan for handlers, validators, and domain-event handlers.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.Scan(s => s.FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .As(implementationType => ClosedInterfacesOf(implementationType, typeof(ICommandHandler<>))).WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .As(implementationType => ClosedInterfacesOf(implementationType, typeof(ICommandHandler<,>))).WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .As(implementationType => ClosedInterfacesOf(implementationType, typeof(IQueryHandler<,>))).WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IValidator<>)), publicOnly: false)
                .As(implementationType => ClosedInterfacesOf(implementationType, typeof(IValidator<>))).WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .As(implementationType => ClosedInterfacesOf(implementationType, typeof(IDomainEventHandler<>))).WithScopedLifetime());

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

        // Queries — innermost first.
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(RetryDecorator.QueryHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(CacheableDecorator.QueryHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(ExceptionDecorator.QueryHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));

        return services;
    }

    /// <summary>
    /// Selects the interfaces on <paramref name="implementationType"/> that close the specified
    /// open generic interface, so a handler is registered only under that closed handler
    /// interface and never under marker interfaces like <see cref="Commands.ICommand"/>.
    /// Scrutor 7.0.0 has no built-in <c>AsClosedTypeOf</c>; <see cref="IServiceTypeSelector.As(Func{Type, IEnumerable{Type}})"/>
    /// is the supported equivalent.
    /// </summary>
    /// <param name="implementationType">The concrete handler type being registered.</param>
    /// <param name="openGenericInterface">The open generic handler interface to close over.</param>
    /// <returns>The closed interfaces to register the implementation type as.</returns>
    private static IEnumerable<Type> ClosedInterfacesOf(Type implementationType, Type openGenericInterface) =>
        implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);
}
