# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run (watch mode)
dotnet watch --project ./Brainy.WebApi

# Run all tests
dotnet test

# Run a single test
dotnet test --filter "ClassName.TestMethodName"

# Run a specific project's tests
dotnet test Brainy.WebApi.IntegrationTests

# EF Core migrations
dotnet ef migrations add {MigrationName} --startup-project ./Brainy.WebApi --project ./Brainy.Infrastructure
dotnet ef database update --startup-project ./Brainy.WebApi --project ./Brainy.Infrastructure
```

CSharpier runs automatically as a pre-commit hook via Husky.Net. To format manually: `dotnet csharpier format .`

## Architecture

Clean Architecture with CQRS across four layers:

- **Brainy.Domain** — Entities, value objects, repository interfaces. No dependencies on other layers.
- **Brainy.Application** — CQRS commands/queries/handlers, DTOs, application services. Depends only on Domain.
- **Brainy.Infrastructure** — EF Core `BrainyContext` (PostgreSQL), repository implementations, email, background jobs. Depends on Domain and Application.
- **Brainy.WebApi** — Controllers, middleware, DI wiring, rate limiting, cookie auth. Depends on all layers.

**Test projects:** `Brainy.Domain.Tests`, `Brainy.Application.Tests`, `Brainy.Infrastructure.Tests`, `Brainy.WebApi.Tests` (unit), `Brainy.WebApi.IntegrationTests` (full HTTP stack with SQLite in-memory).

## CQRS with LiteBus

Commands and queries are mediated via **LiteBus**. All handlers are auto-registered from the `Brainy.Application` assembly.

Feature folders live under `Brainy.Application/{Feature}/Commands/{Name}/` and `.../Queries/{Name}/`. Each folder contains the command/query record and its handler:

```csharp
// Command
public record UpdateUserCommand(UpdateUserInformationDto Dto, Username Username) : ICommand;

// Handler
public class UpdateUserCommandHandler(IUserRepository userRepository) : ICommandHandler<UpdateUserCommand>
{
    public async Task HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default) { ... }
}
```

Queries return a result via `IQuery<TResult>` and `IQueryHandler<TQuery, TResult>`.

## Domain Patterns

**Value objects** extend `ValueObject`, are immutable (`init`), validate in the constructor, and implement `GetEqualityComponents()`. Use them as strongly-typed parameters instead of primitives (`Username`, `Email`, `HashedPassword`, etc.).

**Entities** have a private parameterless constructor for EF Core, immutable IDs/creation properties via `init`, and expose domain logic as methods.

**Repositories** implement `IRepository` (marker interface) and extend `UnitOfWorkRepositoryBase`, which provides `SaveChangesAsync()`. All `IRepository` implementations are auto-registered as Scoped via `AddAllImplementationsForInterface()`. Same pattern applies to `IDomainService` and `IApplicationService`.

## Testing

Unit tests use **MSTest** + **NSubstitute** (mocking) + **AwesomeAssertions** (fluent). Integration tests use `WebApplicationFactory<Program>` with `BrainyWebApplicationFactory`, which swaps the real PostgreSQL `DbContext` for an in-memory SQLite one and auto-creates the schema. Base classes `IntegrationTestBase` and `RepositoryTestBase` handle setup/teardown.

Architecture tests in `Brainy.Domain.Tests` use **NetArchTest.Rules** to enforce layer dependency rules.

Coverage is collected via Coverlet (XPlat format). Minimum threshold is 70%; DTOs, exceptions, and migrations are excluded.

### Test method naming

`MethodName_Scenario_ExpectedResult` — all three segments required:

```csharp
HandleAsync_NewEntitiesSizeExceedsMax_ThrowsInsufficientStorageException
CalculateStorageForEntitiesInBytesAsync_SomeEntityIdsMatch_ReturnsSumOfMatchingEntities
```

### Test structure

Each test is divided into three sections with `// Arrange`, `// Act`, `// Assert` comments. Leave a blank line between sections. For combined Act & Assert (e.g. `ThrowsExactlyAsync`), use a single `// Act & Assert` comment:

```csharp
[TestMethod]
public async Task HandleAsync_Scenario_ExpectedResult()
{
    // Arrange

    ...

    // Act

    var actual = await _handler.HandleAsync(command);

    // Assert

    actual.Should()...;
}
```
