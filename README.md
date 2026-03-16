# Rail Factory Manager

Multi-tenant, microservices-based manufacturing management system. Stack: C#, .NET 8, Blazor, PostgreSQL (one DB per tenant), Redis, RabbitMQ, OAuth2 Google. See [docs/](docs/README.md) for full documentation.

## Quick start

**Infrastructure (Docker):**
```bash
cp .env.example .env   # optional: adjust ports/secrets
docker compose up -d
```

**Build and run a service:**
```bash
dotnet build RailFactory.sln
dotnet run --project src/RailFactory.IAM/RailFactory.IAM.csproj
# Then: https://localhost:5xxx/ and https://localhost:5xxx/health
```

## Repo structure

- **src/RailFactory.Shared** — Tenant resolution abstractions, health model, correlation id, messaging conventions.
- **src/RailFactory.IAM** — Identity and Access (Phase 1); Phase 0 has health, correlation, structured logging.
- **src/RailFactory.Production**, **SupplyChain**, **Logistics**, **Fleet**, **HCM**, **Dashboard** — Service placeholders with health and correlation (Phase 0).

Development order: [docs/08_Development_Order.md](docs/08_Development_Order.md). Infrastructure and env: [docs/10_Infrastructure_And_Env.md](docs/10_Infrastructure_And_Env.md).
