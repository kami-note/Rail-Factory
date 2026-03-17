// Não mexerais em configuração aleia. 

var builder = DistributedApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// Infrastructure (containers)
// -----------------------------------------------------------------------------

var postgres = builder.AddPostgres("postgres").WithPgAdmin();
postgres.AddDatabase("postgresdb");

var iamDb = postgres.AddDatabase("iamdb");

#pragma warning disable ASPIRECERTIFICATES001 // Redis: plain TCP for health checks; image pinned to 7
var redis = builder.AddRedis("redis")
    .WithoutHttpsCertificate()
    .WithImage("redis", "7-alpine");
#pragma warning restore ASPIRECERTIFICATES001

var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// -----------------------------------------------------------------------------
// Parameters (Secrets)
// -----------------------------------------------------------------------------

var googleClientId = builder.AddParameter("GoogleClientId", secret: true);
var googleClientSecret = builder.AddParameter("GoogleClientSecret", secret: true);

// -----------------------------------------------------------------------------
// Backend services
// -----------------------------------------------------------------------------

var iam = builder.AddProject<Projects.RailFactory_Iam_Api>("identity-access-management")
    .WithReference(iamDb)
    .WithEnvironment("Google__ClientId", googleClientId)
    .WithEnvironment("Google__ClientSecret", googleClientSecret)
    .WithEnvironment("Google__RedirectUri", "https://localhost:5001/auth/google/callback")
    .WithEnvironment("Google__FrontendRedirectUri", "https://localhost:3000/auth/callback")
    .WaitFor(iamDb);

// -----------------------------------------------------------------------------
// Gateway (single entry point; runs on host so it can reach IAM via service discovery)
// -----------------------------------------------------------------------------

builder.AddProject<Projects.RailFactory_Gateway>("gateway")
    .WithReference(iam)
    .WaitFor(iam);

builder.Build().Run();
