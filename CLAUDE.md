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
- xUnit v3 on Microsoft.Testing.Platform v2 (`xunit.v3.mtp-v2` 4.x); test
  classes derive from `Benday.Common.Testing.TestClassBase`
- Moq 4.x (4.21.0 or later)
- `Benday.Common.Testing` 4.0.0 from NuGet. 4.0.0 auto-mocks abstract classes
  (`TimeProvider`) in both `CreateInstance<T>()` and `Build<T>()`, and throws
  for concrete classes instead of mocking them. Later stages depend on further
  additions to that library (see "Library dependencies" below).
- `Benday.EfCore` 11.0.11-alpha (and `Benday.EfCore.SqlServer`,
  `Benday.EfCore.Testing`) from stage 04 on. Stages 01-03 use raw EF Core:
  the inherited app doesn't get the nice library, or it stops being the bad
  inherited app. The package is still prerelease, so restores need
  `--prerelease`; a stable release should be cut before the series ships.
- Async throughout, from `01-first-test/before` on: `SaveAsync`,
  `LoadByIdAsync`. That keeps `ThrowsAsync` honest in stage 02 and matches
  the async repository interface in stage 06.
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
04-adapter/
    before/
    after/       PersonToEntityAdapter (AdapterBase) extracted; tested with no mocks
05-clock/
    before/
    after/       TimeProvider through the constructor replaces the GetUtcNow() seam
06-repository/
    before/
    after/       SqlPersonRepository (EfCoreCrudRepositoryBase), MockUtility, in-memory fake
07-validator/
    before/
    after/       IValidatorStrategy<Person>; the Loose-mock bool gotcha
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
| `Benday.MoqDemo.Business` | `Person`, `PersonManager` (the system under test throughout), `Logger`, `SqlServerOutOfDiskSpaceException`, `PersonToEntityAdapter` (stage 04+), `PersonValidator` (stage 07). |
| `Benday.MoqDemo.DataAccess` | EF Core `PersonDbContext`, `PersonEntity` (stages 01-05), `PersonDataAccess` (stages 01-05), `SqlPersonRepository` (stage 06+). |
| `Benday.MoqDemo.Common` | From stage 03: `ILogger`. From stage 06: `PersonEntity`, so Business can talk to the repository without referencing DataAccess. From stage 06 on, Business and DataAccess both reference Common, never each other. |
| `Benday.MoqDemo.UnitTests` | xUnit. `PersonManagerFixture.cs` is the file that changes in every stage and is where the story is told. |

Every `before` and `after` must `dotnet build` and `dotnet test` green from
its own folder before it is considered done (a `before` with no test project
yet just has to build). Integration tests that need SQL Server live in a
separate `Benday.MoqDemo.IntegrationTests` project (from stage 06, when
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

`PersonManager` with `SaveAsync(Person)` and `LoadByIdAsync(int)`. `SaveAsync`
stamps `CreateDate` and `LastModified` with `DateTime.UtcNow`, copies the
fields onto a `PersonEntity` by hand, then news up `PersonDataAccess` inline
and calls it. `LoadByIdAsync` does the same in reverse. Logging is
`new Logger().WriteToLog(...)` inline. `PersonDataAccess` talks to EF Core
directly and speaks `PersonEntity` (DataAccess can't see `Person`, because
Business references DataAccess). No interfaces, no constructor parameters,
no tests, no `Benday.EfCore`. The console app works end to end against SQL
Server in the container.

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
   minimal and save the interface for stage 06).
3. Test: `new Mock<PersonManager> { CallBase = true }`, override
   `CreateDataAccess` via `Protected().Setup<PersonDataAccess>("CreateDataAccess")`
   to return a `Mock<PersonDataAccess>().Object`, override `GetUtcNow` to a
   fixed date. Assert `CreateDate` and `LastModified` are set to that date and
   that `SaveAsync` on the data access was called once.

Stages 01-03 stay in plain Moq permanently, string-based `Protected()` calls
included: raw Moq is what those episodes teach, and the library helpers are
the payoff in stage 06 on. Don't retrofit them when helpers ship. The one
planned exception is after the talk: once `Ambient<T>` ships in
`Benday.Common.Refactoring`, add a second test here showing the same thing
done with `Ambient<PersonDataAccess>.Replace(...)` and no `virtual` at all;
keep both so the two first-test techniques can be compared.

### 02-exceptions (`after`)

The "mock it because you can't produce the condition" stage.

- Test that forces `Save` into the `SqlServerOutOfDiskSpaceException` handler
  by making the mocked data access throw it (`.ThrowsAsync(...)`).
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

### 04-adapter (`after`)

The "don't mock what converts the shapes" stage. The adapter comes before
the repository because `Benday.EfCore` repositories store entities: the
service adapts, the repository stores. That replaces the book's line that
"the service never sees an EF entity."

- `before` already has the setup: `Benday.EfCore` referenced from Business
  and DataAccess, `PersonEntity : EntityBase`, `Person : DomainModelBase`
  (the adapter's constraints need both). `EntityBase` adds no mapped
  columns, so the shared `MoqDemo` database keeps working.
- Extract the hand-copying in `PersonManager` into
  `PersonToEntityAdapter : AdapterBase<Person, PersonEntity>` in Business.
- `PersonManager` creates it with `new`. It is **not** injected: a mocked
  adapter silently copies nothing, which is the failure the talk warns
  about, and `CreateInstance` (4.0.0) refuses concrete constructor
  parameters anyway. Pure logic, so use the real one.
- `PersonToEntityAdapterFixture` tests both directions and the Id rule (the
  database owns `PersonEntity.Id`, so Person-to-entity never copies it) with
  real objects and **no mocks**; `FakeValueGenerator.Create<T>()` from
  `Benday.Common.Testing` populates the source object.
- `PersonManagerFixture` doesn't change; it still uses the partial mock.

### 05-clock (`after`)

- Replace the `GetUtcNow()` seam with `TimeProvider` through the constructor:
  `PersonManager(ILogger, TimeProvider)`. The console app passes
  `TimeProvider.System`.
- Tests construct the partial mock with constructor arguments
  (`new Mock<PersonManager>(logger.Object, timeProvider) { CallBase = true }`),
  since the data access seam is still the stage-01 one.
- Show both test clocks: `FakeTimeProvider` from
  `Microsoft.Extensions.TimeProvider.Testing`, and a Moq mock of
  `TimeProvider` (`Setup(x => x.GetUtcNow())`).
- This comes before the repository on purpose: once stage 06 removes the
  partial mock, a `GetUtcNow()` seam could no longer be overridden and the
  timestamp test would have no way to pin the date.

### 06-repository (`after`)

- `PersonEntity` moves to Common. `SqlPersonRepository :
  EfCoreCrudRepositoryBase<PersonEntity, PersonDbContext>` in DataAccess
  absorbs the EF Core code from `PersonDataAccess`, which is deleted.
- `PersonManager` takes `IAsyncReadableRepository<PersonEntity, int>` (from
  `Benday.Common.Interfaces`), `ILogger` and `TimeProvider`. Use the library
  interface directly rather than a custom `IPersonRepository`, so
  `InMemoryRepository<PersonEntity>` from `Benday.EfCore.Testing` fits
  without a wrapper. The partial-mock seams are gone: no more `virtual`, no
  more `[Seam]`, no more `CreateDataAccess()`.
- `FixDiskSpaceProblem()` becomes private, so the disk-space test now asserts
  that the exception propagates and the logger was called once, with
  `ThrowsAsync` on the repository mock.
- Tests: every test becomes `MockUtility.CreateInstance<PersonManager>()` and
  configures only the mocks it cares about via `GetRequiredMock<T>()`;
  `TimeProvider` is auto-mocked (that's what 4.0.0 added). This is the stage
  where the arrange blocks visibly shrink; keep a copy of the stage-05
  version of one test in the README for the before/after.
- One test uses `InMemoryRepository<PersonEntity>` instead of a mock, to make
  the "mocks for interactions, fakes for state" point.

### 07-validator (`after`)

- `PersonValidator : IValidatorStrategy<Person>` in Business, holding rules
  that would otherwise live in `PersonManager.SaveAsync`. `PersonManager`
  takes `IValidatorStrategy<Person>` through the constructor and refuses to
  save (throws `InvalidObjectException`) when it returns false.
- Tests mock the validator (`Returns(false)`, verify the repository's
  `SaveAsync` is `Times.Never` called; `Returns(true)`, verify it's called).
  Separate `PersonValidatorFixture` tests the real rules with real `Person`
  objects, no mocks.
- Show the gotcha on purpose: adding a `bool` dependency to an auto-mocked
  class makes every happy-path test fail because a Loose mock returns
  `false`. Fix with a `Returns(true)` setup (or `SetupForAnyArgs(...).ReturnsTrue()`
  when it ships). Contrast with `FakeValidatorStrategy<T>` from
  `Benday.EfCore.Testing`, which defaults to `true`.

At the end of stage 07 there are no `virtual` seams, no `[Seam]` attributes,
no `Ambient<T>` calls, and no `DateTime.UtcNow` anywhere in Business.

## Library dependencies

These stages reference additions to `Benday.Common.Testing` (and the new
`Benday.Common.Refactoring` package) that are planned in
`/Users/benday/code/benday-inc/Benday.Common/TESTING-TODO.md`. This repo is
also the integration test bed for those additions. When one ships:

| Addition | Used in |
| --- | --- |
| Abstract-class auto-mocking (`TimeProvider`) | 06 (**shipped in 4.0.0**) |
| `ShouldHaveBeenCalled*` verify extensions | 06, 07 |
| `GetSingleCallArgument<TArg>` capture | 06 (assert on the saved entity and the logged exception) |
| `SetupForAnyArgs` / `ReturnsTrue` | 07 |
| `CreatePartial<T>` / `SetupProtected` | not used in the demo: 01-03 stay plain Moq (library-only) |
| `Ambient<T>`, `[Seam]` (Refactoring package) | 01, second test; after the talk |

Build order: stages 01-03 first in plain Moq, then a `Benday.Common.Testing`
feature release shaped by what hurt in those stages, then stages 04-07. In
stages 06-07, until an addition ships, write the plain-Moq form and mark it
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
| 04 | Segment 2: "don't mock the adapter" | Ep. 5: Don't mock the adapter |
| 05 | Mentioned with the five moves | Ep. 6: Faking the clock |
| 06 | Segment 5: `MockUtility` | Ep. 7: Repository and the setup tax |
| 07 | Segment 5: the validator gotcha | Ep. 8: The validator and the bool trap |

The talk plan with timings and speaker notes is in the Claude Project doc
"Mocking with Moq, 20-Minute Talk Plan". The deck carries code slides for
stages 02, 03, 06 and 07, so it stands on its own without a live demo; the
one live moment is the stage-07 `bool` gotcha, typed into a
`prepare-demo.ps1` scratch folder. The rest is "it's on the playlist."
The deck has a QR code for the YouTube playlist right after the five moves
and again at the close.

## Conventions for this repo

- Test class per class under test, named `{ClassName}Fixture` (so
  `PersonManagerFixture`, `PersonToEntityAdapterFixture`), deriving from
  `TestClassBase`. Each fixture exposes the instance under test through a
  `SystemUnderTest` property (lazily created; from stage 06 on it wraps a
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
  deliberate `bool`-default failure in `07`. They are the demo.
- Commit per stage with a message that names the stage and the test that
  proves the change.
