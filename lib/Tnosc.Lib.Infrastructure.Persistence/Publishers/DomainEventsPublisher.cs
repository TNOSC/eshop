// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Infrastructure.Persistence.Publishers;

internal sealed class DomainEventsPublisher(IServiceProvider serviceProvider)
    : IDomainEventsPublisher
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeDictionary = new();
    private static readonly ConcurrentDictionary<Type, Type> WrapperTypeDictionary = new();

    public async ValueTask PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            using IServiceScope scope = serviceProvider.CreateScope();

            Type domainEventType = domainEvent.GetType();
            Type handlerType = HandlerTypeDictionary.GetOrAdd(
                key: domainEventType,
                valueFactory: et => typeof(IDomainEventHandler<>).MakeGenericType(typeArguments: et));

            IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(serviceType: handlerType);

            foreach (object? handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                var handlerWrapper = HandlerWrapper.Create(handler: handler, domainEventType: domainEventType);

                await handlerWrapper.HandleAsync(domainEvent: domainEvent, cancellationToken: cancellationToken);
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
