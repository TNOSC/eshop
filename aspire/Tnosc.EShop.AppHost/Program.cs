// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args: args);

// WithDataVolume persists Postgres data across restarts. A schema change during development may
// therefore require dropping the volume manually before the next run picks it up.
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres(name: "postgres")
    .WithDataVolume()
    .WithPgAdmin();

IResourceBuilder<PostgresDatabaseResource> db = postgres.AddDatabase(name: "eshopdb");

builder.AddProject<Projects.Tnosc_EShop_Server_Host>(name: "eshop-host")
    .WithReference(source: db)
    .WaitFor(dependency: db);

await builder.Build().RunAsync();
