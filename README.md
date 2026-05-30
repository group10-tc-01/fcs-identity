# Fcg.Identity

API generated with the **Modular Clean Architecture** template.

## Projects

| Project | Description |
|---|---|
| `Domain` | Entities, domain exceptions, repository interfaces |
| `Application` | Use cases, CQRS abstractions, validation |
| `Messages` | Shared message contracts (events) |
| `Infrastructure.Auth` | JWT authentication, BCrypt password hashing _(optional)_ |
| `Infrastructure.SqlServer` | EF Core + SQL Server persistence _(optional)_ |
| `Infrastructure.PostgreSql` | EF Core + PostgreSQL persistence _(optional)_ |
| `Infrastructure.MongoDb` | MongoDB.Driver persistence _(optional)_ |
| `Infrastructure.Kafka` | Kafka message publishing via Confluent.Kafka _(optional)_ |
| `WebApi` | ASP.NET Core Web API — controllers, middleware, DI |
| `CommomTestsUtilities` | Shared test builders and fakes |
| `UnitTests` | Use case unit tests |
| `IntegratedTests` | Controller integration tests |
| `FunctionalTests` | BDD scenarios with Reqnroll |

---

## Getting started

### Install the template

```bash
dotnet new install ./path/to/DotnetCleanArchitecture.TemplatePack.csproj
```

Or from NuGet (once published):

```bash
dotnet new install DotnetCleanArchitecture.Templates
```

### Create a new project

```bash
dotnet new cleanarchapi -n MyCompany.MyService
```

This generates the project inside a `MyCompany.MyService/` folder using the default options:
SQL Server + MediatR + FluentValidation + Serilog + OpenTelemetry + Swagger + Auth.

---

## Template options

All options are boolean flags. Pass `--<flag>` to enable or `--<flag> false` to disable.

### Persistence (multiple allowed)

| Flag | Default | Description |
|---|---|---|
| `--useSqlServer` | `true` | EF Core with SQL Server |
| `--usePostgreSql` | `false` | EF Core with PostgreSQL |
| `--useMongoDB` | `false` | MongoDB.Driver |

### Messaging (multiple allowed)

| Flag | Default | Description |
|---|---|---|
| `--useKafka` | `false` | Confluent.Kafka message publisher |

### Libraries

| Flag | Default | Description |
|---|---|---|
| `--useMediatR` | `true` | MediatR for CQRS dispatch. When disabled, use cases are injected directly into controllers |
| `--useFluentValidation` | `true` | FluentValidation for request validation |
| `--useSerilog` | `true` | Serilog for structured logging |
| `--useOpenTelemetry` | `true` | OpenTelemetry tracing and metrics |
| `--useSwagger` | `true` | Swashbuckle + API versioning |
| `--useAuth` | `true` | JWT Bearer authentication + BCrypt |
| `--useCiCd` | `true` | GitHub Actions wrappers for `fcg-pipelines` reusable CI/CD workflows |

### CI/CD naming

When CI/CD is enabled, pass `--serviceSlug` to define the service name used by workflows,
container image names, Kubernetes resource names, and the SonarCloud project key.

```bash
dotnet new cleanarchapi -n Fcg.Identity \
  --serviceSlug fcg-identity
```

---

## Usage examples

### Minimal — SQL Server only, all libs enabled (default)

```bash
dotnet new cleanarchapi -n MyCompany.MyService
```

### PostgreSQL instead of SQL Server

```bash
dotnet new cleanarchapi -n MyCompany.MyService \
  --useSqlServer false \
  --usePostgreSql
```

### Multiple databases — SQL Server + MongoDB

```bash
dotnet new cleanarchapi -n MyCompany.MyService \
  --useMongoDB
```

### PostgreSQL + Kafka

```bash
dotnet new cleanarchapi -n MyCompany.MyService \
  --useSqlServer false \
  --usePostgreSql \
  --useKafka
```

### All databases + Kafka

```bash
dotnet new cleanarchapi -n MyCompany.MyService \
  --usePostgreSql \
  --useMongoDB \
  --useKafka
```

### Without authentication

```bash
dotnet new cleanarchapi -n MyCompany.MyService \
  --useAuth false
```

### Without CI/CD workflows

```bash
dotnet new cleanarchapi -n MyCompany.MyService \
  --useCiCd false
```

### Minimal setup — no observability, no swagger, no auth

```bash
dotnet new cleanarchapi -n MyCompany.MyService \
  --useAuth false \
  --useSwagger false \
  --useSerilog false \
  --useOpenTelemetry false
```

### Full example — MongoDB + Kafka, no MediatR, no auth

```bash
dotnet new cleanarchapi -n MyCompany.MyService \
  --useSqlServer false \
  --useMongoDB \
  --useKafka \
  --useMediatR false \
  --useAuth false
```

---

## Running the project

```bash
# Restore and build
dotnet restore
dotnet build

# Run all tests
dotnet test

# Start with Docker Compose (spins up selected databases and Seq)
docker compose up -d
dotnet run --project src/Fcg.Identity.WebApi
```

---

## Project structure

```
src/
  Fcg.Identity.Domain/
  Fcg.Identity.Application/
  Fcg.Identity.Messages/
  Fcg.Identity.Infrastructure.Keycloak/   # Keycloak integration
  Fcg.Identity.Infrastructure.SqlServer/  # present if --useSqlServer
  Fcg.Identity.Infrastructure.Kafka/      # present if --useKafka
  Fcg.Identity.WebApi/
tests/
  Fcg.Identity.CommomTestsUtilities/
  Fcg.Identity.UnitTests/
  Fcg.Identity.IntegratedTests/
  Fcg.Identity.FunctionalTests/
```
