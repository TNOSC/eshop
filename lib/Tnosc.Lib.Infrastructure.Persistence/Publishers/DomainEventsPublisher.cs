// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tnosc.Lib.Application.Decorators;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Infrastructure.Persistence.Publishers;

internal sealed class DomainEventsPublisher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventsPublisher> logger)
    : IDomainEventsPublisher
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeDictionary = new();
    private static readonly ConcurrentDictionary<Type, Type> WrapperTypeDictionary = new();

    public async ValueTask<DomainEventDeliveryReport> PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        List<DomainEventHandlerFailure> failures = [];

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            // CreateAsyncScope, not CreateScope: a handler's scope can hold services that implement
            // only IAsyncDisposable — IUnitOfWork does, and the inbox decorator resolves it — and
            // IServiceScope.Dispose() throws for those. Disposing synchronously would surface as the
            // event failing *after* its handler already succeeded.
            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

            foreach (object handler in ResolveHandlers(scope: scope, domainEventType: domainEvent.GetType()))
            {
                await InvokeAsync(
                    handler: handler,
                    domainEvent: domainEvent,
                    failures: failures,
                    cancellationToken: cancellationToken);
            }
        }

        return failures.Count == 0 ? DomainEventDeliveryReport.Success : new DomainEventDeliveryReport(failures: failures);
    }

    public async ValueTask PublishToHandlerAsync(IDomainEvent domainEvent, string handlerName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: domainEvent);

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        foreach (object handler in ResolveHandlers(scope: scope, domainEventType: domainEvent.GetType()))
        {
            if (!string.Equals(a: HandlerChain.NameOf(handler: handler), b: handlerName, comparisonType: StringComparison.Ordinal))
            {
                continue;
            }

            var wrapper = HandlerWrapper.Create(handler: handler, domainEventType: domainEvent.GetType());

            await wrapper.HandleAsync(domainEvent: domainEvent, cancellationToken: cancellationToken);

            return;
        }

        throw new InvalidOperationException(
            message: $"No handler named '{handlerName}' is registered for domain event type '{domainEvent.GetType()}'.");
    }

    /// <summary>
    /// Runs one handler, recording rather than propagating a failure so the handlers after it still run.
    /// </summary>
    private async ValueTask InvokeAsync(
        object handler,
        IDomainEvent domainEvent,
        List<DomainEventHandlerFailure> failures,
        CancellationToken cancellationToken)
    {
        string handlerName = HandlerChain.NameOf(handler: handler);

        try
        {
            var wrapper = HandlerWrapper.Create(handler: handler, domainEventType: domainEvent.GetType());

            await wrapper.HandleAsync(domainEvent: domainEvent, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown, not a handler defect: leave the message unprocessed for the next poll rather
            // than counting it as a failure against this handler.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                exception: ex,
                message: "Handler {Handler} failed for domain event {EventId}; remaining handlers still run.",
                handlerName,
                domainEvent.Id);

            failures.Add(item: new DomainEventHandlerFailure(
                EventId: domainEvent.Id,
                HandlerName: handlerName,
                Exception: ex));
        }
    }

    private static IEnumerable<object> ResolveHandlers(AsyncServiceScope scope, Type domainEventType)
    {
        Type handlerType = HandlerTypeDictionary.GetOrAdd(
            key: domainEventType,
            valueFactory: et => typeof(IDomainEventHandler<>).MakeGenericType(typeArguments: et));

        foreach (object? handler in scope.ServiceProvider.GetServices(serviceType: handlerType))
        {
            if (handler is not null)
            {
                yield return handler;
            }
        }
    }

    private abstract class HandlerWrapper
    {
        public abstract ValueTask HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);

        public static HandlerWrapper Create(object handler, Type domainEventType)
        {
            Type wrapperType = WrapperTypeDictionary.GetOrAdd(
                key: domainEventType,
                valueFactory: et => typeof(HandlerWrapper<>).MakeGenericType(typeArguments: et));

            return (HandlerWrapper)(Activator.CreateInstance(type: wrapperType, args: handler)
                ?? throw new InvalidOperationException(message: $"Failed to create a handler wrapper for domain event type '{domainEventType}'."));
        }
    }

    private sealed class HandlerWrapper<T>(object handler) : HandlerWrapper where T : IDomainEvent
    {
        private readonly IDomainEventHandler<T> _handler = (IDomainEventHandler<T>)handler;

        public override async ValueTask HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await _handler.HandleAsync(@event: (T)domainEvent, cancellationToken: cancellationToken);
        }
    }
}
