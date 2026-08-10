// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// WithDataVolume persists Postgres data across restarts. A schema change during development may
// therefore require dropping the volume manually before the next run picks it up.
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

IResourceBuilder<PostgresDatabaseResource> db = postgres.AddDatabase("eshopdb");

builder.AddProject<Projects.Tnosc_EShop_Server_Host>("eshop-host")
    .WithReference(db)
    .WaitFor(db);

await builder.Build().RunAsync();
