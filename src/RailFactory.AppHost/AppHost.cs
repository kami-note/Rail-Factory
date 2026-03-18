// Não mexerais em configuração aleia. 

var builder = DistributedApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// Infrastructure (containers)
// -----------------------------------------------------------------------------

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();
postgres.AddDatabase("postgresdb");

var iamDb = postgres.AddDatabase("iamdb");

#pragma warning disable ASPIRECERTIFICATES001 // Redis: plain TCP for health checks; image pinned to 7
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithoutHttpsCertificate()
    .WithImage("redis", "7-alpine");
#pragma warning restore ASPIRECERTIFICATES001

var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// -----------------------------------------------------------------------------
// Parameters (Secrets)
// -----------------------------------------------------------------------------

var googleClientId = builder.AddParameter("GoogleClientId", secret: true);
var googleClientSecret = builder.AddParameter("GoogleClientSecret", secret: true);
var googleRedirectUri = builder.AddParameter("GoogleRedirectUri");
var googleFrontendRedirectUri = builder.AddParameter("GoogleFrontendRedirectUri");

// -----------------------------------------------------------------------------
// Backend services
// -----------------------------------------------------------------------------

var iam = builder.AddProject<Projects.RailFactory_Iam_Api>("identity-access-management")
    .WithReference(iamDb)
    .WithReference(redis)
    .WithEnvironment("Google__ClientId", googleClientId)
    .WithEnvironment("Google__ClientSecret", googleClientSecret)
    .WithEnvironment("Google__RedirectUri", googleRedirectUri)
    .WithEnvironment("Google__FrontendRedirectUri", googleFrontendRedirectUri)
    .WaitFor(iamDb);

// -----------------------------------------------------------------------------
// Frontend (Blazor)
// -----------------------------------------------------------------------------

var frontend = builder.AddProject<Projects.RailFactory_Frontend>("frontend");

// -----------------------------------------------------------------------------
// Gateway (single entry point)
// -----------------------------------------------------------------------------

builder.AddProject<Projects.RailFactory_Gateway>("gateway")
    .WithReference(iam)
    .WithReference(frontend)
    .WaitFor(iam)
    .WaitFor(frontend);

builder.Build().Run();
