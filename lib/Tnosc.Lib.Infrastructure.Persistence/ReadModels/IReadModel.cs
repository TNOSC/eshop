// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Marks a class as a query-side read model, mapped by a read <c>DbContext</c> and never by a write one.
/// </summary>
/// <remarks>
/// A type must not implement both <see cref="IReadModel"/> and <c>IAggregateRoot</c> — they select
/// disjoint halves of the composed EF model, so a type implementing both would map to both contexts.
/// </remarks>
public interface IReadModel;
