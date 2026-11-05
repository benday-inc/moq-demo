# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this repo is

Demo code for Benjamin Day's 2026 talk **"Mocking with Moq: A Quick Guide to
Smarter Unit Tests"** (20-minute conference session) and the companion YouTube
series that walks the same code at full length.

It is a port of the `Benday.FakesSample` solution from the TechEd 2014 talk
DEV-B358 "Zero to Hero: Untested to Tested with Microsoft Fakes" to .NET 10,
xUnit, Moq, and `Benday.Common.Testing`. The 2014 original lives at
`/Users/benday/code/repos/zero-to-hero`; read its `DEMO-SUMMARY.md` first for
the arc and the sample app. That repo is .NET Framework 4.5 / Visual Studio
2013 / MSTest / Microsoft Fakes / typed DataSets / WinForms and does not build
on macOS. Nothing here depends on building it. Port the *design* and the
*narrative*, not the code.

The arc, unchanged since 2014: you inherit a tightly coupled, untestable app.
Get a first test around it with the smallest possible change. Then refactor
toward interfaces, constructor injection, Repository, and Adapter until the
tests need no tricks at all. What changed since 2014: Microsoft Fakes shims
and stubs are replaced by Moq, hand-wired mocks are replaced by
`MockUtility`, and the first-test trick is extract-and-override / `Ambient<T>`
instead of a profiler.

## Stack

- .NET 10 (`net10.0`), C# with nullable reference types enabled, file-scoped
  namespaces
- xUnit for tests; test classes derive from `Benday.Common.Testing.TestClassBase`
- Moq 4.x
- `Benday.Common.Testing` from NuGet. Start on the current published version
  (3.2.0). Several stages depend on additions being made to that library
  (see "Library dependencies" below); until those ship, write the plain-Moq
  version and leave a `// TODO(library): replace with X` comment.
- Data access: EF Core against SQL Server running in Docker. Start it with
  `./start-sql-server-container.ps1` in the repo root (image
  `mcr.microsoft.com/mssql/server:2025-latest`, container `sql_server`,
  port 1433, SA password `Pa$$word`; `-Refresh` to pull and recreate).
  Each stage's console app creates its database (`MoqDemo`) with
  `EnsureCreated()` on startup. The connection string lives in ONE place per
  stage, `appsettings.json` in the console app, so there's nothing to hunt
  for on stage. A real SQL Server is deliberate: the talk's premise is that
  the database dependency is expensive, and the integration tests should
  feel that. Unit tests never touch it; if Docker isn't running, the console
  app and integration tests fail and the unit tests still pass, which is
  the point.
- UI: a console app (`Benday.MoqDemo.ConsoleUI`) is the composition root. The
  2014 WinForms `MainForm` becomes a few menu options (create, load, save).
  The UI exists to show where real dependencies get wired; it is not the demo.

Assertions: use the fluent `Benday.Common.Testing` assertions with a
descriptive message on every call (`result.ShouldEqual(expected, "why")`).
Plain `Assert.*` from xUnit only where the library has no equivalent.

## Repo layout: before/after folders per stage

Same structure as the 2014 repo. Each stage has a `before` and an `after`
folder, and each folder is a complete, independent, buildable solution.
`after` of stage N is the starting point for `before` of stage N+1 (copied,
then any pre-typed scaffolding the live demo should skip is added to
`before`). This duplicates code on purpose: any stage opens cold, before and
after can sit side by side on screen, and a later fix to one stage does not
rewrite history. Git tags are not used for stages.

```
01-first-test/
    before/      the inherited app: no tests, no seams (this is the 2014 "Demo 0" state)
    after/       extract-and-override + partial mock; the first green test
02-exceptions/
    before/
    after/       SqlServerOutOfDiskSpaceException, ThrowsAsync, Times.Never
03-logger-di/
    before/
    after/       ILogger through the constructor; verify behavior
04-repository/
    before/
    after/       IRepository<IPerson>, in-memory repository, MockUtility
05-adapter-validator/
    before/
    after/       PersonToEntityAdapter tests with no mocks; IPersonValidator; TimeProvider
shared/          nothing executable; slide-ready snippets and README text
prepare-demo.ps1 copies every before/ into a dated scratch folder (see below)
start-sql-server-container.ps1   starts the SQL Server container
```

`01-first-test/before` is the untested inherited app described under
"The starting point" below; the stage numbering counts demos, not states.

### Demo-day scratch folders

Live demos are never done in the stage folders. `prepare-demo.ps1` creates
`./demo-{yyyyMMdd}/` and copies each stage's `before` folder into it as
`demo-{yyyyMMdd}/01-first-test/`, `.../02-exceptions/`, and so on, then runs
`dotnet build` on each copy so the first F5 on stage isn't a restore. Type
into the copies; the stage folders stay pristine. `.gitignore` excludes
`demo-????????/`, so scratch folders never get committed. Run it again the
next day for a fresh set; delete old ones freely. It also checks that the
`sql_server` container is running and warns if not.

Each `before` and `after` folder contains `Benday.MoqDemo.slnx` and:

| Project | Role |
| --- | --- |
| `Benday.MoqDemo.ConsoleUI` | Composition root. Constructs `PersonManager` with real dependencies. |
| `Benday.MoqDemo.Business` | `Person`, `PersonManager` (the system under test throughout), `Logger`, `SqlServerOutOfDiskSpaceException`, later `PersonValidator`. |
| `Benday.MoqDemo.DataAccess` | EF Core `PersonDbContext`, `PersonEntity`, `PersonDataAccess` (stages 1-3), `SqlPersonRepository` (stage 4+), `PersonToEntityAdapter` (stage 5). |
| `Benday.MoqDemo.Common` | From stage 3: `IPerson`, `ILogger`, `IRepository<T>`, `IPersonValidator`. Business and DataAccess both reference Common, never each other. |
| `Benday.MoqDemo.UnitTests` | xUnit. `PersonManagerFixture.cs` is the file that changes in every stage and is where the story is told. |

Every `before` and `after` must `dotnet build` and `dotnet test` green from
its own folder before it is considered done (a `before` with no test project
yet just has to build). Integration tests that need SQL Server live in a
separate `Benday.MoqDemo.IntegrationTests` project (from stage 04, when
`SqlPersonRepository` exists) so `dotnet test` on the unit test project
never needs Docker.

**The ten-minute rule.** Each stage is one video, and each `before` to
`after` diff must be typeable on camera in roughly ten minutes. If a stage's
diff is bigger than that, split it into two stages rather than making the
video longer. Keep every change minimal; move any scaffolding the demo
should skip into the next stage's `before`. A stage's README.md (one per stage folder, above
`before`/`after`) states in a few lines what changes between the two and
which test proves it.

## The stages in detail

### The starting point (`01-first-test/before`)

`PersonManager` with `Save(Person)` and `LoadById(int)`. `Save` stamps
`CreateDate` and `LastModified` with `DateTime.UtcNow`, then news up
`PersonDataAccess` inline and calls it. `LoadById` does the same. Logging is
`new Logger().WriteToLog(...)` inline. `PersonDataAccess` talks to EF Core
directly. No interfaces, no constructor parameters, no tests. The console
app works end to end against SQL Server in the container.

Deliberately ugly in the 2014 way: a `try/catch` in `Save` with two handlers,
one for `SqlServerOutOfDiskSpaceException` (calls `FixDiskSpaceProblem()`,
logs, rethrows) and one for `Exception` (logs, rethrows). Nothing throws the
first one. That's the point.

### 01-first-test (`after`)

The 2014 Demo 1 used Fakes shims to replace `new PersonDataAccess()` with no
production change. The 2026 version makes the smallest production change
that creates a seam, then mocks through it.

1. In `PersonManager`, extract `protected virtual PersonDataAccess CreateDataAccess() => new PersonDataAccess();`
   and `protected virtual DateTime GetUtcNow() => DateTime.UtcNow;`. Mark both
   `[Seam]` if the attribute exists by then; otherwise a comment that says
   "seam for tests; do not inline".
2. Make `PersonDataAccess` members `virtual` so Moq can override them (or
   extract `IPersonDataAccess`; prefer `virtual` here to keep the change
   minimal and save the interface for stage 4).
3. Test: `new Mock<PersonManager> { CallBase = true }`, override
   `CreateDataAccess` via `Protected().Setup<PersonDataAccess>("CreateDataAccess")`
   to return a `Mock<PersonDataAccess>().Object`, override `GetUtcNow` to a
   fixed date. Assert `CreateDate` and `LastModified` are set to that date and
   that `SavePerson` was called once.

Once `MockUtility.CreatePartial<T>()` and `SetupProtected` ship, replace the
string-based `Protected()` calls. Once `Ambient<T>` ships in
`Benday.Common.Refactoring`, add a second test showing the same thing done
with `Ambient<PersonDataAccess>.Replace(...)` and no `virtual` at all; keep
both so the two first-test techniques can be compared.

### 02-exceptions (`after`)

The "mock it because you can't produce the condition" stage.

- Test that forces `Save` into the `SqlServerOutOfDiskSpaceException` handler
  by making the mocked data access throw it (`.Throws(...)` / `.ThrowsAsync(...)`).
  Assert `FixDiskSpaceProblem` was called (make it `protected virtual`, verify
  via the partial mock) and the exception propagates.
- Sibling test for the generic `Exception` handler.
- Speaker line this stage exists for: "the only non-mock way to test this is a
  utility that starts SQL Server and fills the disk with garbage."

### 03-logger-di (`after`)

- Extract `ILogger` from `Logger` into the new Common project.
- `PersonManager` gets a constructor taking `ILogger`; null-check; the inline
  `new Logger()` calls become `_logger.WriteToLog(ex)`. The console app
  constructs `new PersonManager(new Logger())`.
- Tests verify *behavior*: `logger.Verify(x => x.WriteToLog(It.IsAny<Exception>()), Times.Once)`
  in the exception tests, `Times.Never` in the happy-path test. When the
  library's `ShouldHaveBeenCalledOnce` / `ShouldNotHaveBeenCalled` ship,
  switch to those.
- The data access seam is still the stage-1 partial mock. That's deliberate:
  this stage is about injecting one dependency, not all of them.

### 04-repository (`after`)

- `IPerson` and `IRepository<T>` (with `GetById`, `Save`, `Delete`, `GetAll`,
  async) move to Common. `Person` implements `IPerson`.
- `SqlPersonRepository : IRepository<IPerson>` in DataAccess absorbs the EF
  Core code that was in `PersonManager`/`PersonDataAccess`. `PersonDataAccess`
  is deleted.
- `PersonManager` shrinks to the business rules: constructor takes
  `IRepository<IPerson>` and `ILogger`; the partial-mock seams from stage 1
  are removed (no more `virtual`, no more `[Seam]`); `GetUtcNow` stays for now.
- Tests: every test becomes `var sut = MockUtility.CreateInstance<PersonManager>();`
  and configures only the mocks it cares about via `GetRequiredMock<T>()`.
  This is the stage where the arrange blocks visibly shrink; keep a copy of
  the stage-3 version of one test in the README for the before/after.
- Also show the in-memory alternative: one test uses
  `Benday.Common.Testing.InMemoryReadableRepository` (or a local
  `InMemoryRepository<T>` if the interface shapes don't line up) instead of a
  mock, to make the "mocks for interactions, fakes for state" point.

### 05-adapter-validator (`after`)

Three additions, each a short video on its own.

- **Adapter.** `PersonToEntityAdapter` with `Adapt(PersonEntity, IPerson)` and
  `Adapt(IPerson, PersonEntity)`; the second only copies `Id` when the target
  has none. `SqlPersonRepository` uses it. `PersonToEntityAdapterFixture`
  tests both directions and the Id rule with real objects and **no mocks**;
  use `FakeValueGenerator` from the library to populate the source object.
  This is the "don't mock what converts the shapes" example.
- **Validator.** `IPersonValidator.IsValid(IPerson)` extracted from rules that
  would otherwise live in `PersonManager.Save`; `PersonValidator` in Business.
  `PersonManager` takes it through the constructor and refuses to save when
  it returns false. Tests mock the validator (`Returns(false)`, verify no
  save; `Returns(true)`, verify save). Separate `PersonValidatorFixture`
  tests the real rules with real `Person` objects, no mocks.
  Show the gotcha on purpose: adding a `bool` dependency to an auto-mocked
  class makes every happy-path test fail because a Loose mock returns
  `false`. Fix with a `Returns(true)` setup (or `SetupForAnyArgs(...).ReturnsTrue()`
  when it ships).
- **Clock.** Replace the `GetUtcNow()` seam with `TimeProvider` through the
  constructor. Tests use `FakeTimeProvider` from
  `Microsoft.Extensions.TimeProvider.Testing`, or a Moq mock of
  `TimeProvider`. Requires `MockInstanceBuilder` to auto-mock abstract
  classes; until that ships, pass it explicitly with
  `MockUtility.Build<PersonManager>().WithValue<TimeProvider>(fake).Build()`.

At the end of stage 5 there are no `virtual` seams, no `[Seam]` attributes,
no `Ambient<T>` calls, and no `DateTime.UtcNow` anywhere in Business.

## Library dependencies

These stages reference additions to `Benday.Common.Testing` (and the new
`Benday.Common.Refactoring` package) that are planned in
`/Users/benday/code/benday-inc/Benday.Common/TESTING-TODO.md`. This repo is
also the integration test bed for those additions. When one ships:

| Addition | Used in |
| --- | --- |
| `ShouldHaveBeenCalled*` verify extensions | 02, 03, 04, 05 |
| `GetSingleCallArgument<TArg>` capture | 03 (assert on the logged exception), 04 (assert on the saved person) |
| `SetupForAnyArgs` / `ReturnsTrue` | 05 validator |
| Abstract-class auto-mocking (`TimeProvider`) | 05 clock |
| `CreatePartial<T>` / `SetupProtected` | 01, 02 |
| `Ambient<T>`, `[Seam]` (Refactoring package) | 01 (second test and seam annotations) |

Until an addition ships, write the plain-Moq form and mark it
`// TODO(library): ...` so the swap is a grep away. Never add a
`ProjectReference` to the Benday.Common repo from here; this repo consumes
published packages only (a local NuGet feed is fine during development:
`copy-packages-to-local-nuget.ps1` in that repo).

## Mapping to the talk and the videos

| Stage | Talk segment (20 min) | Video |
| --- | --- | --- |
| 01 before | Segment 1 opener: "how would you test this?" | Ep. 1: The inherited app |
| 01 | Mentioned in Q&A ("monolith with no DI") | Ep. 2: The first test |
| 02 | Segment 2 + 4: the disk-space story and test | Ep. 3: Testing the impossible |
| 03 | Segment 3: the five Moq moves | Ep. 4: Inject, then verify |
| 04 | Segment 5: `MockUtility` | Ep. 5: Repository and the setup tax |
| 05 | Segment 2 + 4: validator; "don't mock the adapter" | Ep. 6: Adapter, validator, clock |

The talk plan with timings and speaker notes is in the Claude Project doc
"Mocking with Moq, 20-Minute Talk Plan". The talk shows stages 02, 03, 04 and
the validator half of 05 live, from a `prepare-demo.ps1` scratch folder; the rest is "it's on the playlist."

## Conventions for this repo

- Test class per class under test, named `{ClassName}Fixture` (so
  `PersonManagerFixture`, `PersonToEntityAdapterFixture`), deriving from
  `TestClassBase`. Each fixture exposes the instance under test through a
  `SystemUnderTest` property (lazily created; from stage 04 on it wraps a
  `MockCreationResult<T>` so `SystemUnderTest.Instance` and
  `SystemUnderTest.GetRequiredMock<T>()` read naturally). This is the
  convention from the 2014 and 2017 talks and the one the audience will have
  seen before.
- Test names are sentences a product owner could nod at:
  `Save_WhenPersonIsInvalid_DoesNotCallRepository`.
- One assertion idea per test. Arrange/act/assert separated by blank lines,
  no comments saying "arrange".
- Keep demo code small. Every line on screen costs time; if a method needs
  scrolling it needs splitting.
- Leave the previous stage's version of a changed test in that stage's
  README, not commented out in the code (the 2014 repo commented out old
  bodies for side-by-side; here the side-by-side is two folders).
- Do not "fix" the deliberate ugliness in `01-first-test/before` or the
  deliberate `bool`-default failure in `05`. They are the demo.
- Commit per stage with a message that names the stage and the test that
  proves the change.
