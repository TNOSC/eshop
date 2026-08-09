// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;

namespace Tnosc.Lib.Infrastructure.Persistence.Extensions;

/// <summary>
/// Configures <see cref="PersistenceExtensions.AddPersistence{TWriteContext, TReadContext}"/>.
/// </summary>
public sealed class PersistenceOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistenceOptions"/> class with placeholder
    /// values for its required members.
    /// </summary>
    /// <remarks>
    /// This constructor exists only so <c>AddPersistence</c> can create an instance to hand to the
    /// caller-supplied configuration delegate. <see cref="ConnectionName"/>, <see cref="ConfigurationAssembly"/>,
    /// and <see cref="DomainEventAssemblies"/> are still required in practice — <c>AddPersistence</c>
    /// validates that the delegate replaced them before use.
    /// </remarks>
    [SetsRequiredMembers]
    public PersistenceOptions()
    {
        ConnectionName = string.Empty;
        ConfigurationAssembly = typeof(PersistenceOptions).Assembly;
        DomainEventAssemblies = [];
    }

    /// <summary>
    /// Gets the Aspire connection resource name used to resolve the Postgres connection string
    /// (for example, <c>"eshopdb"</c>).
    /// </summary>
    public required string ConnectionName { get; init; }

    /// <summary>
    /// Gets the assembly scanned for <c>IEntityTypeConfiguration&lt;T&gt;</c> classes on both the
    /// write and read models.
    /// </summary>
    public required Assembly ConfigurationAssembly { get; init; }

    /// <summary>
    /// Gets the assemblies scanned for concrete domain event types, used to build the
    /// <see cref="IDomainEventTypeRegistry"/>.
    /// </summary>
    public required Assembly[] DomainEventAssemblies { get; init; }

    /// <summary>
    /// Gets the assembly containing EF Core migrations, if different from <see cref="ConfigurationAssembly"/>.
    /// </summary>
    public Assembly? MigrationsAssembly { get; init; }

    /// <summary>
    /// Gets a value indicating whether pending migrations are applied automatically on startup.
    /// Intended for Development only.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; init; }

    /// <summary>
    /// Gets a value indicating whether the outbox background processor is registered. Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableOutboxProcessing { get; init; } = true;

    /// <summary>
    /// Gets the outbox processor's batching, polling, and retry settings.
    /// </summary>
    public OutboxOptions Outbox { get; } = new();
}
