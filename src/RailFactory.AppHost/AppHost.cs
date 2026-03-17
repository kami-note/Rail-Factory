var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure (containers)
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();
var postgresDb = postgres.AddDatabase("postgresdb");

// WithoutHttpsCertificate: Aspire enables TLS on Redis by default; health check and clients use plain TCP → SSL errors.
// Pin to Redis 7 for consistency with docker-compose and to avoid Redis 8 TLS defaults.
#pragma warning disable ASPIRECERTIFICATES001 // WithoutHttpsCertificate is evaluation API; we need plain TCP for Redis.
var redis = builder.AddRedis("redis")
    .WithoutHttpsCertificate()
    .WithImage("redis", "7-alpine");
#pragma warning restore ASPIRECERTIFICATES001

var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// Add project references so each microservice starts with the AppHost (see docs 08 and 10).

builder.Build().Run();
