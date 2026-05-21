# Sincera Policies API

Auto-insurance policy management — starter solution for the live coding session.

## Prerequisites

- .NET 8 SDK (pinned via `global.json`)
- Any IDE: Rider, Visual Studio 2022, or VS Code with C# Dev Kit

## Run

```
dotnet run --project src/Sincera.Policies.Api
```

Then open <http://localhost:5080/> — the root redirects to Scalar's API reference UI at `/scalar/v1`. The OpenAPI document is served at `/openapi/v1.json`. The database is SQLite in-memory and is reseeded on every startup.

## Test

```
dotnet test
```

On a clean clone you should see 47 tests passing.

## What's in here

| Project | Purpose |
|---------|---------|
| `Sincera.Policies.Domain` | Entities, value objects, the calculators (`PremiumCalculator`, `RenewalCreditCalculator`, `CancellationPolicy`). Pure — no framework references. |
| `Sincera.Policies.Application` | MediatR commands/queries/handlers, FluentValidation, pipeline behaviors (validation, logging), repository interfaces, Mapperly mappers. |
| `Sincera.Policies.Infrastructure` | EF Core + SQLite in-memory, repositories, unit of work with domain-event dispatch, notification gateway (logs only), `IClock`. |
| `Sincera.Policies.Api` | Minimal API endpoints, ProblemDetails error mapping, Serilog, Scalar API reference UI. |
| `tests/*.UnitTests` | xUnit + NSubstitute + FluentAssertions. |
| `tests/*.IntegrationTests` | `WebApplicationFactory<Program>` with mocked repositories. |

## Seed data

The database is reseeded on every startup. You can hit these out of the box.

| Policy   | Customer | Status    | Notes                                            |
|----------|----------|-----------|--------------------------------------------------|
| P-1001   | Anna     | Active    | 90 days in, mixed coverage, 2 renewals           |
| P-1002   | Mikus    | Active    | 15 days fresh, young driver, single coverage     |
| P-1003   | Rita     | Active    | 180 days in, full coverage, has open claims      |
| P-1004   | Anna     | Cancelled | Was cancelled 30 days ago                        |
| P-1005   | Anna     | Draft     | Not yet activated — try `POST /api/policies/P-1005/activate` |
| P-1006   | Rita     | Expired   | Term ended 35 days ago                           |

Customers: `C-9001` Anna, `C-9002` Mikus, `C-9003` Rita, `C-9004` Edgars (no policies).

## Conventions

- **Time** comes from `IClock`. Never use `DateTime.Now` / `DateTime.UtcNow` directly in domain or handler code — tests rely on a fake clock.
- **Money** is `decimal`. Premiums round through `PremiumCalculator.RoundPremium` — use it to stay consistent.
- **State changes on aggregates** happen through methods on the aggregate (e.g., `policy.Activate(...)`). Don't reach in from a handler.
- **Domain events** are raised on aggregates and dispatched by `UnitOfWork.SaveChangesAsync` after persistence.
- **Configuration** is bound via `IOptions<>` — `PremiumOptions`, `CancellationOptions`, `ClaimsOptions` are wired in `appsettings.json`.
- **Endpoints** are minimal APIs registered through extension methods (see `PolicyEndpoints.MapPolicyEndpoints`). Add new routes in the same file or a sibling extension.
- **Mapping** between domain entities and response DTOs goes through Mapperly source-generated mappers (`PolicyMapper`). Don't hand-roll mapping in handlers — extend the mapper.
- **Data access** goes through repository interfaces. The `DbContext` is registered in Infrastructure and should not leak into Application or Api code; `IDatabaseInitializer` handles startup `EnsureCreated` + seed without exposing EF Core.

## Notes for the live session

- A task brief will be handed to you at the start of the session. Read it, then look around the code before typing.
- AI tools (Claude Code, Copilot, etc.) are allowed. Use them however helps you — narrating your decisions matters more than how you got there.
- The SQLite database is in-memory and tied to a singleton connection opened on startup. Don't introduce EF migrations during the session; they don't survive an in-memory connection cycle.
- If `dotnet test` is green and the new endpoint behaves correctly via Scalar (`http://localhost:5080/scalar/v1`), you're done. No need to chase coverage beyond what's meaningful.
