### Coding Conventions (as observed in this codebase)

- Language/Target
  - .NET: net9.0
  - C# with file-scoped namespaces (e.g., `namespace PersonalAgent;`).
- Naming
  - Classes, interfaces, methods: PascalCase (`ChatService`, `InitializeAsync`).
  - Interfaces prefixed with `I` (`IChatService`, `IMemoryService`, `IDatabaseService`).
  - Private fields: `_camelCase` with leading underscore (`_logger`, `_settings`).
  - Async methods end with `Async` suffix (`ProcessUserInputAsync`, `InitializeDatabaseAsync`).
- Structure within files
  - `using` directives at top; project/third-party usings grouped together (no explicit regioning observed).
  - File-scoped namespace, then interface (when applicable), then implementation class.
  - Dependency Injection (constructor injection) for services and configuration via `IOptions<T>`.
- Logging
  - Use `Microsoft.Extensions.Logging` abstractions with generic category logger (`ILogger<T>`).
  - Log levels: `Information` for normal flow, `Error` in catch blocks.
- Error handling
  - Try/catch around top-level program flow and service entry points. Re-throw in initialization failures when appropriate.
- Configuration
  - Strongly-typed settings via `AppSettings` bound from `appsettings.json` using `services.Configure<AppSettings>(configuration)`. Nested settings classes: `ConnectionStrings`, `OllamaSettings`, `ApplicationSettings`.
- Data Access
  - Dapper with Npgsql for PostgreSQL. SQL kept inline in service (`DatabaseService`), using string interpolation only for trusted config values (e.g., table name).
- Plugins/Extensibility
  - Semantic Kernel plugins registered via `ImportPluginFromObject` and `Plugins.AddFromType<T>()`.
- Style & Formatting
  - Braces on new lines for types/methods; 4-space indentation; expression/collection initializers used.
  - Interpolated verbatim/raw strings (`$""" ... """`) for multiline prompts/SQL.


### Code Organization and Package Structure

- Solution: single project `PersonalAgent` (console app).
- High-level folders and responsibilities
  - `Configuration/`
    - Strongly-typed app configuration classes (e.g., `AppSettings.cs`).
  - `Model/`
    - Domain models/value objects (e.g., `MemoryNote.cs`).
  - `Services/`
    - Application services and infrastructure services.
    - Each service typically defines an interface and an implementation in the same file (e.g., `IChatService`/`ChatService`).
    - Examples:
      - `ChatService` — orchestrates Semantic Kernel chat, prompt composition, streaming, and function calling.
      - `MemoryService` — domain-specific memory operations and persistence orchestration.
      - `DatabaseService` — database bootstrapping (schema creation) and low-level data access with Dapper/Npgsql.
  - `Plugin/`
    - Semantic Kernel plugins to extend assistant capabilities (e.g., `MathPlugin`, `TimePlugin`, `MemoryPlugin`).
  - Root files
    - `Program.cs` — application bootstrap: host builder, DI setup, logging, configuration, lifecycle (`Initialize*`, chat loop).
    - `appsettings.json` — runtime configuration (connection strings, model IDs, limits, etc.).
    - `PROJECT_OVERVIEW_FOR_REVIEW.md`, `WARP.md` — documentation.

- Namespaces mirror folders
  - `PersonalAgent.Configuration`, `PersonalAgent.Services`, `PersonalAgent.Model`, `PersonalAgent.Plugin`.

- Dependency Injection graph (at startup)
  - Config bound to `AppSettings`.
  - Services registered as scoped: `IDatabaseService`, `IMemoryService`, `IChatService`.
  - Logging configured via console provider with minimum level `Information`.


### Unit and Integration Testing Approaches

Note: No test project is currently in the repository. The following outlines recommended approaches that align with the current architecture and patterns.

- Test Frameworks & Libraries
  - xUnit as the test framework (common default for .NET).
  - FluentAssertions for expressive assertions.
  - Moq (or NSubstitute) for mocking dependencies.
  - Testcontainers for .NET for integration tests that require PostgreSQL.

- Project Layout (recommended)
  - Create a separate test project: `PersonalAgent.Tests` targeting `net9.0`.
  - Folder structure inside tests mirroring production namespaces: `Services`, `Plugin`, `Configuration`, `Model`.

- Unit Testing Guidelines
  - General principles
    - Keep tests deterministic, isolated, and fast. Avoid real network or DB calls in unit tests.
    - Mock external dependencies: `ILogger<T>`, `IMemoryService`, `IDatabaseService`, and Semantic Kernel abstractions.
  - `ChatService`
    - Mock the Semantic Kernel chat completion service interface used through DI (`IChatCompletionService`) and any kernel/plugin interactions where feasible.
    - Verify prompt composition includes expected sections and that streaming results are aggregated into the final assistant message.
    - Ensure errors in streaming are caught and logged, and that user feedback is printed.
  - `MemoryService`
    - Validate business rules for creating/searching/listing/deleting `MemoryNote` items.
    - If `MemoryService` relies on `IDatabaseService` for persistence, mock those calls.
  - `DatabaseService`
    - For unit scope, verify SQL generation decisions that don’t require DB (e.g., table name selection from config). For actual DB operations use integration tests.
  - Configuration binding
    - Use `ConfigurationBuilder` with in-memory JSON to verify `AppSettings` binding behavior (including nested classes like `OllamaSettings`).

- Integration Testing Guidelines
  - Database (PostgreSQL)
    - Use `Testcontainers.PostgreSql` to spin up a disposable PostgreSQL instance per test class/collection.
    - Apply `DatabaseService.InitializeDatabaseAsync()` and assert that the expected table exists with correct schema.
    - Exercise typical workflows through `MemoryService` against the real DB: insert/list/search/delete and validate behavior, respecting limits from `ApplicationSettings`.
  - End-to-end chat path (optional, limited scope)
    - With Semantic Kernel mocked or pointed to a lightweight local model, verify that entering a message triggers function-calling decisions (e.g., calling `MemoryPlugin`) and that conversation history is updated.

- Example: xUnit test skeletons

```csharp
// PersonalAgent.Tests/Services/DatabaseServiceTests.cs
public class DatabaseServiceTests
{
    [Fact]
    public async Task InitializeDatabaseAsync_CreatesTable()
    {
        // Arrange: use Testcontainers to start PostgreSQL and build IOptions<AppSettings>
        // Act: call InitializeDatabaseAsync
        // Assert: query information_schema to verify table exists
    }
}
```

```csharp
// PersonalAgent.Tests/Services/ChatServiceTests.cs
public class ChatServiceTests
{
    [Fact]
    public async Task ProcessUserInputAsync_StreamsAndLogsResponse()
    {
        // Arrange: mock IChatCompletionService to yield streaming content parts
        // Assert: ensure final content is aggregated and history updated
    }
}
```

- Continuous Integration considerations
  - Mark integration tests with a category/trait (e.g., `[Trait("Category", "Integration")]`) and optionally exclude them by default in local runs.
  - Provide environment-variable configuration for connection strings and model endpoints if not using Testcontainers.

- Test Data & Fixtures
  - Use xUnit fixtures (`IClassFixture<>` / `ICollectionFixture<>`) to share expensive setup like Testcontainers PostgreSQL across multiple tests safely.


### Additional Recommendations (optional, non-blocking)

- Consider enabling nullable reference types in `PersonalAgent.csproj` (`<Nullable>enable</Nullable>`) if not already enabled.
- Consider extracting SQL to dedicated helpers or using parameterized Dapper calls for future non-constant inputs.
- If the plugin surface expands, consider moving each interface and class to separate files for discoverability, and add XML/markdown docs for plugin contracts.
