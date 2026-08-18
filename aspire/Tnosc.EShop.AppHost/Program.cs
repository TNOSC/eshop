// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args: args);

// WithDataVolume persists Postgres data across restarts. A schema change during development may
// therefore require dropping the volume manually before the next run picks it up.
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres(name: "postgres")
    .WithDataVolume()
    .WithUrlForEndpoint(endpointName: "tcp", callback: url => url.DisplayText = "Postgres")
    .WithPgAdmin(configureContainer: pgAdmin =>
        pgAdmin.WithUrlForEndpoint(endpointName: "http", callback: url => url.DisplayText = "pgAdmin"));

IResourceBuilder<PostgresDatabaseResource> db = postgres.AddDatabase(name: "eshopdb");

// Two jobs: the basket store (one JSON document per customer, TTL'd) and the L2 backing store for
// HybridCache solution-wide. WithDataVolume is deliberate — baskets surviving a container restart is
// what a developer expects — and carries the same caveat as Postgres's: a document-shape change may
// need the volume dropped before the next run behaves.
IResourceBuilder<RedisResource> cache = builder.AddRedis(name: "cache")
    .WithDataVolume()
    .WithUrlForEndpoint(endpointName: "tcp", callback: url => url.DisplayText = "Redis")
    .WithRedisInsight(configureContainer: redisInsight =>
        redisInsight.WithUrlForEndpoint(endpointName: "http", callback: url => url.DisplayText = "RedisInsight"));

// Keycloak gets its OWN database on this same Postgres server rather than a schema inside eshopdb.
// Same server, same volume, same credentials — but its ~90 Liquibase-managed tables never appear in
// eshopdb, so EF migrations, Respawn's schema reset and the outbox are all unaffected by them.
IResourceBuilder<PostgresDatabaseResource> keycloakDb = postgres.AddDatabase(name: "keycloakdb");

// WithRealmImport seeds the eshop realm — but --import-realm is a NO-OP once that realm already
// exists in the persisted keycloakdb. Editing Realms/eshop-realm.json after the first successful run
// therefore changes nothing until the realm is deleted in the admin console or the Postgres data
// volume is dropped. Same gotcha as the schema-change comment above, for the same reason.
// AddPostgres always provisions a password parameter, generating one when the caller supplies none.
// The property is typed nullable for the general case; failing loudly here beats a null-forgiving
// operator, because a null would otherwise surface as Keycloak failing to authenticate to Postgres.
ParameterResource postgresPassword = postgres.Resource.PasswordParameter
    ?? throw new InvalidOperationException(message: "AddPostgres did not provision a password parameter.");

// Keycloak dials Postgres container-to-container, so KC_DB_URL must resolve to the container-network
// address rather than localhost. PrimaryEndpoint.Property(HostAndPort) is what yields the former; a
// plain endpoint URL taken from the dashboard would yield the latter and Keycloak would fail to boot.
// The fixed host port is for humans — curl, the admin console, the hosted login page. The API itself
// never uses it: service discovery resolves Keycloak over the container network.
// If port 8080 is already taken on the host, DCP cannot allocate it and the Keycloak resource simply
// never starts — no error is logged, the container just never appears. Symptom: everything else comes
// up and Keycloak is missing. Fix: free the port, or change the number here.
//
// KC_HOSTNAME/KC_HOSTNAME_PORT pin Keycloak to mint ONE canonical issuer
// (http://localhost:8080/realms/eshop) regardless of who is asking. Without this, the API resolves
// Keycloak's authority via service discovery (the container-network address) while the web BFF's
// browser-facing OIDC flow is pinned to localhost:8080 for the same reason a browser cannot resolve a
// container hostname — and a token minted at one issuer fails validation against the other with a 401
// that looks like a token bug rather than a hostname mismatch.
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak(name: "keycloak", port: 8080)
    .WithRealmImport(import: "./Realms")
    .WithEnvironment(name: "KC_DB", value: "postgres")
    .WithEnvironment(
        name: "KC_DB_URL",
        value: ReferenceExpression.Create(
            $"jdbc:postgresql://{postgres.Resource.PrimaryEndpoint.Property(property: EndpointProperty.HostAndPort)}/keycloakdb"))
    .WithEnvironment(name: "KC_DB_USERNAME", value: postgres.Resource.UserNameReference)
    .WithEnvironment(name: "KC_DB_PASSWORD", value: ReferenceExpression.Create($"{postgresPassword}"))
    .WithEnvironment(name: "KC_HOSTNAME", value: "localhost")
    .WithEnvironment(name: "KC_HOSTNAME_PORT", value: "8080")
    .WaitFor(dependency: keycloakDb)
    .WithUrlForEndpoint(endpointName: "http", callback: url => url.DisplayText = "Keycloak Admin Console");

IResourceBuilder<ProjectResource> eshopHost = builder.AddProject<Projects.Tnosc_EShop_Server_Host>(name: "eshop-host")
    .WithReference(source: db)
    .WithReference(source: keycloak)
    .WithReference(source: cache)
    .WaitFor(dependency: db)
    .WaitFor(dependency: keycloak)
    .WaitFor(dependency: cache)
    .WithUrlForEndpoint(endpointName: "https", callback: url => url.DisplayText = "API (Scalar)");

builder.AddProject<Projects.Tnosc_EShop_Client_Web>(name: "eshop-web")
    .WithReference(source: eshopHost)
    .WithReference(source: keycloak)
    .WaitFor(dependency: eshopHost)
    .WaitFor(dependency: keycloak)
    .WithExternalHttpEndpoints()
    .WithUrlForEndpoint(endpointName: "https", callback: url => url.DisplayText = "eShop Web");

await builder.Build().RunAsync();
