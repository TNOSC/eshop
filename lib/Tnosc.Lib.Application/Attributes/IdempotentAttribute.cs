// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Application.Attributes;

/// <summary>
/// Marks a handler whose effects must happen <b>at most once</b> per key, however many times the
/// message is delivered.
/// </summary>
/// <remarks>
/// <para>
/// On a command handler the key is the caller's <c>Idempotency-Key</c>, taken from
/// <see cref="Observabilities.IdempotencyKeyContext"/>; a repeat of the same key replays the
/// original response instead of running the handler again. A marked command handler whose key is
/// missing <b>fails</b> rather than degrading silently — the attribute is opt-in, so its author
/// asked for the guarantee.
/// </para>
/// <para>
/// On a domain event handler the key is <see cref="Tnosc.Lib.Domain.IDomainEvent.Id"/>, which closes
/// the at-least-once redelivery window described on
/// <see cref="DomainEvents.IDomainEventHandler{TEvent}"/>: a redelivered event is recognised and
/// skipped instead of applying its effect twice.
/// </para>
/// <para>
/// Both cases commit the claim record in the <b>same transaction</b> as the handler's own writes, so
/// there is no window in which a key is burned without its effect, or an effect lands without its
/// key. See <c>IdempotencyDecorator</c> for the mechanics.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IdempotentAttribute : Attribute { }
