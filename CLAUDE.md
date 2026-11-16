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

**Two separate stories** (restructured 2026-11-11):

- **The mocking demos (`01`-`07`)** are about mocking. Each one is a testing
  problem that a mock solves, and each one injects one more dependency
  through `PersonManager`'s constructor (`03` is the exception: it adds no
  dependency, it contrasts the mock from `02` with an in-memory fake). They use plain `Mock<T>` on
  interfaces from the first mock on. **No partial mocks, no `CallBase`, no
  `Protected()`, no `virtual` seams anywhere in `01`-`07`.** That material
  cluttered the mocking story and confused viewers, so it was removed.
- **The refactoring demo (`refactoring/`)** stands alone. It's the 2014
  "first test" story: an inherited app that creates its own dependencies,
  extract-and-override, and a partial mock. Its purpose is refactoring legacy
  code, not mocking, and nothing in `01`-`07` builds on it.

## Stack

- .NET 10 (`net10.0`), C# with nullable reference types enabled, file-scoped
  namespaces
- xUnit v3 on Microsoft.Testing.Platform v2 (`xunit.v3.mtp-v2` 4.x); test
  classes derive from `Benday.Common.Testing.TestClassBase`
- Moq 4.x (4.21.0 or later)
- `Benday.Common.Testing` 4.0.0 from NuGet. 4.0.0 auto-mocks abstract classes
  (`TimeProvider`) in both `CreateInstance<T>()` and `Build<T>()`, and throws
  for concrete classes instead of mocking them. That's the last library
  change the demo needs (see "Library dependencies" below).
- `Benday.EfCore` 11.0.11-alpha (and `Benday.EfCore.SqlServer`,
  `Benday.EfCore.Testing`) in demos `01`-`07` and `extras/`. The
  `refactoring/` demo uses raw EF Core: the inherited app doesn't get the
  nice library. The package is still prerelease, so restores need
  `--prerelease`; a stable release should be cut before the series ships.
- Async throughout: `SaveAsync`, `LoadByIdAsync`.
- Data access: EF Core against SQL Server, login `sa` / `Pa$$word`,
  connection strings `Server=localhost;...` with no port. On macOS that's
  the Docker container: start it with `./start-sql-server-container.ps1` in
  the repo root (image `mcr.microsoft.com/mssql/server:2025-latest`,
  container `sql_server`, host port 1433, which must stay 1433 because the
  connection strings name no port; `-Refresh` to pull and recreate). On
  Windows it's the default SQL Server instance with the same `sa` login;
  leaving the port off lets SqlClient use shared memory, so TCP/IP doesn't
  have to be enabled.
  Each console app creates its database (`MoqDemo`) with `EnsureCreated()` on
  startup. The connection string lives in ONE place per folder,
  `appsettings.json` in the console app, so there's nothing to hunt for on
  stage. From demo 02, the integration test project has its own
  `appsettings.json` pointing at a separate `MoqDemoTests` database, so test
  runs never touch the demo data. A real SQL Server is deliberate: the talk's
  premise is that the database dependency is expensive, and the integration
  tests should feel that. Unit tests never touch it; if SQL Server isn't
  reachable, the console app and integration tests fail and the unit tests
  still pass, which is the point.
- UI: a console app (`Benday.MoqDemo.ConsoleUI`) is the composition root. The
  2014 WinForms `MainForm` becomes a few menu options (create, load, save).
  The UI exists to show where real dependencies get wired; it is not the demo.

Assertions: use the fluent `Benday.Common.Testing` assertions with a
descriptive message on every call (`result.ShouldEqual(expected, "why")`).
Plain `Assert.*` from xUnit only where the library has no equivalent.

## Repo layout: before/after folders per demo

Each demo has a `before` and an `after` folder, and each folder is a
complete, independent, buildable solution. `after` of demo N is the starting
point for `before` of demo N+1 (copied, then any pre-typed scaffolding the
live demo should skip is added to `before`). This duplicates code on purpose:
any demo opens cold, before and after can sit side by side on screen, and a
later fix to one demo does not rewrite history. Git tags are not used.

Folder names say the **concept**, not the class that changes:

```
01-adapter/
    before/      the inherited app: no tests, PersonManager creates everything it uses
    after/       PersonToEntityAdapter (AdapterBase) extracted; tested with no mocks
02-data-access/
    before/
    after/       repository interface through the constructor; Mock<T>, .Object, ReturnsAsync, It.IsAny, Callback
03-fake-repository/
    before/      adds the Benday.EfCore.Testing package as setup
    after/       InMemoryRepository<PersonEntity>: mocks for scripted answers, fakes for state; tests only
04-exceptions/
    before/
    after/       ThrowsAsync; tests only, no production change
05-verification/
    before/
    after/       ILogger through the constructor; Verify, Times.Once / Never
06-clock/
    before/
    after/       TimeProvider through the constructor; Mock<TimeProvider> and FakeTimeProvider
07-validation/
    before/      adds the MockUtility twin fixture as setup
    after/       IValidatorStrategy<Person>; the Loose-mock bool gotcha
refactoring/     standalone: extract-and-override + partial mock (the 2014 "first test")
    before/
    after/
extras/
    setup-tax/   standalone, no before/after: six-dependency class, plain Moq vs MockUtility
prepare-demo.ps1 copies every before/ into a dated scratch folder (see below)
start-sql-server-container.ps1   starts the SQL Server container
.editorconfig    one file at the root (from `dotnet new editorconfig`); every demo folder inherits it, and prepare-demo.ps1 copies it into each scratch stage so a zipped copy keeps it
```

**Solution files are named for the folder they're in**, so IDE title bars and
recent-file lists tell them apart: `Benday.MoqDemo-04-exceptions-before.slnx`,
`Benday.MoqDemo-04-exceptions-after.slnx`,
`Benday.MoqDemo-refactoring-before.slnx`, `Benday.MoqDemo-setup-tax.slnx`.
Project names and namespaces do **not** carry the demo name; they are
`Benday.MoqDemo.*` everywhere, so code on slides never changes.

### Demo-day scratch folders

Live demos are never done in the demo folders. `prepare-demo.ps1` creates
`./demo-{yyyyMMdd}/` and copies each numbered demo's `before` folder into it
as `demo-{yyyyMMdd}/01-adapter/`, `.../02-data-access/`, and so on, plus
`refactoring/before` as `demo-{yyyyMMdd}/refactoring/` and every
`extras/<name>` folder as `demo-{yyyyMMdd}/extras/<name>/`, then runs
`dotnet build` on each copy so the first F5 on stage isn't a restore. Type
into the copies; the demo folders stay pristine. `.gitignore` excludes
`demo-????????/`, so scratch folders never get committed. Run it again the
next day for a fresh set; delete old ones freely. It also checks that the
`sql_server` container is running and warns if not.

### Projects

| Project | Role |
| --- | --- |
| `Benday.MoqDemo.ConsoleUI` | Composition root. Constructs `PersonManager` with real dependencies. |
| `Benday.MoqDemo.Business` | `Person`, `PersonManager` (the system under test throughout), `Logger`, `SqlServerOutOfDiskSpaceException`, `PersonToEntityAdapter` (01 after), `PersonValidator` (07). |
| `Benday.MoqDemo.DataAccess` | EF Core `PersonDbContext`, `PersonDataAccess` (until 02 after deletes it), `SqlPersonRepository` (from 02 before). |
| `Benday.MoqDemo.Common` | From 02 before: `PersonEntity`, so Business can talk to the repository without referencing DataAccess. From 05 after: `ILogger`. From 02 after on, Business and DataAccess both reference Common, never each other. |
| `Benday.MoqDemo.UnitTests` | xUnit. `PersonManagerFixture.cs` (from 02 after) is the file that changes in every demo and is where the story is told. `TestConstants.cs` holds shared test values. |
| `Benday.MoqDemo.IntegrationTests` | From 02 before. `SqlPersonRepositoryFixture` (the real repository) and `PersonManagerIntegrationFixture` (`PersonManager` end to end) against a real SQL Server. The constructor call in `PersonManagerIntegrationFixture.CreatePersonManager()` grows with `PersonManager`'s constructor in every demo. Separate project so `dotnet test` on the unit tests never needs Docker. |

Every `before` and `after` must `dotnet build` clean and `dotnet test` green
on the unit test project from its own folder before it is considered done.
One exception: `01-adapter/before` has a unit test project with no tests yet,
so `dotnet test` there reports "Zero tests ran" (exit code 8). It only has to
build.

**The ten-minute rule.** Each demo is one video, and each `before` to
`after` diff must be typeable on camera in roughly ten minutes. If a demo's
diff is bigger than that, split it rather than making the video longer. Keep
every change minimal; move any scaffolding the demo should skip into that
demo's `before`. A demo's README.md (one per demo folder, above
`before`/`after`) opens with the search question the demo answers (the video
title), a **Goal** section (what the demo is trying to achieve and why it
matters) and a **How** section (the technique, in plain words), then **What
changes** between `before` and `after` and which tests prove it. Write
READMEs as if they'll be published: each one becomes the episode's companion
blog post.

## The demos in detail

`PersonManager`'s constructor grows by one parameter per demo:
`()` in 01, `(repository)` from 02, `(repository, logger)` from 05,
`(repository, logger, timeProvider)` from 06, and
`(repository, logger, validator, timeProvider)` in 07 after. Demo 03 adds
nothing to the constructor.

### 01-adapter

The "don't mock what converts the shapes" demo, and Part 1 of the two-part
"how do I unit test database calls" video. It comes first because
`Benday.EfCore` repositories store entities: the service adapts, the
repository stores. The adapter has to exist before the database can go behind
a repository interface in demo 02.

- `before` is the inherited app: `PersonManager` with `SaveAsync(Person)` and
  `LoadByIdAsync(int)`. It news up `PersonDataAccess` inline, stamps dates
  with `DateTime.UtcNow`, logs with `new Logger().WriteToLog(...)` inline, and
  copies fields onto `PersonEntity` by hand. Problem spots are labeled
  "Problem: ...". The `try/catch` in `SaveAsync` has two handlers, one for
  `SqlServerOutOfDiskSpaceException` (calls `FixDiskSpaceProblem()`, logs,
  rethrows) and one for `Exception` (logs, rethrows). Nothing throws the first
  one. That's the point. Setup already in place: `Benday.EfCore` referenced,
  `PersonEntity : EntityBase`, `Person : DomainModelBase`, and an empty unit
  test project with `TestConstants.cs` and no Moq reference.
- `after` extracts the copying into
  `PersonToEntityAdapter : AdapterBase<Person, PersonEntity>` in Business.
  `PersonManager` creates it with `new`. It is **not** injected: a mocked
  adapter silently copies nothing, which is the failure the talk warns about,
  and `CreateInstance` (4.0.0) refuses concrete constructor parameters anyway.
- `PersonToEntityAdapterFixture` tests both directions and the Id rule (the
  database owns `PersonEntity.Id`, so Person-to-entity never copies it) with
  real objects and **no mocks**; `FakeValueGenerator.Create<T>()` populates
  the source object.

### 02-data-access

The "infrastructure dependency" demo, and Part 2 of the two-parter. This is
where Moq first appears.

- `before` adds scaffolding: the Common project with `PersonEntity`,
  `SqlPersonRepository : EfCoreCrudRepositoryBase<PersonEntity, PersonDbContext>`
  (unused), the integration test project (`SqlPersonRepositoryFixture` plus
  `PersonManagerIntegrationFixture`, which drives the inherited
  `PersonManager` against SQL Server and is the safety net for the refactor),
  and Moq in the unit tests. Only Moq: the fake library arrives in 03, so
  nothing in 02 can discover it early.
- `after`: `PersonManager` takes `IAsyncReadableRepository<PersonEntity, int>`
  (from `Benday.Common.Interfaces`). Use the library interface directly
  rather than a custom `IPersonRepository`, so `InMemoryRepository<PersonEntity>`
  from `Benday.EfCore.Testing` fits without a wrapper in 03. `PersonDataAccess`
  is deleted and Business stops referencing DataAccess.
- On camera, adding the constructor breaks the console app and the
  integration fixture, and both get fixed right then with the real
  `SqlPersonRepository`. **No transitional parameterless constructor and no
  `null!`**: that detour is what made the first recording confusing. The
  first unit test then fails at runtime (it reaches for SQL Server), and each
  method is refactored onto `_repository` with a test watching.
- `SaveExistingAsync` keeps the stored record's `CreateDate`
  (`saveThis.CreateDate = entity.CreateDate` after the load). Without it the
  adapter copied the caller's `CreateDate`, usually `default`, over the real
  one. The test for it is the centerpiece of the demo, because only a mock can
  hand back "a stored record with this exact CreateDate" for free.
- Tests: `Mock<T>`, `.Object`, `Setup(...).ReturnsAsync(...)`, then
  `It.IsAny<T>()` with `Callback(...)` to capture the entity handed to
  `SaveAsync` and to play the database (stamp `Id = 42`). Seven tests: load
  found and not found; new save copies the fields, assigns the Id, populates
  both dates on the `Person` and the entity; update keeps the stored
  `CreateDate`; update of an unknown Id throws. The logger and clock are
  still inline, so the date tests check that the dates were set, not what
  they were set to; 06 pins them.

### 03-fake-repository

The "mock or fake?" demo. Tests only; no production change. It's the
`InMemoryRepository` episode, and it exists to say when Moq is the wrong tool.

- `before` is 02's `after` plus the `Benday.EfCore.Testing` package.
- A `Mock<T>` is a scripted answer with no memory; a fake is a working
  implementation with a shortcut. Save-then-load with a mock needs a
  `Callback` to capture and stamp an Id plus a second `Setup` to hand it back
  (that scripted version is typed on camera and kept in the README, not in the
  fixture). With `InMemoryRepository<PersonEntity>` it's one line.
- Tests added to `PersonManagerFixture`:
  `LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson` (the same name as the
  integration test, on purpose: same test, `List<T>` instead of SQL Server)
  and `SaveAsync_WhenCalledTwice_UpdatesTheStoredPerson` (two saves, one
  record in `Items`, `CreateDate` untouched).
- Rule of thumb the episode lands on: mock when you need an answer you can't
  produce (null, an exception, a time, "invalid"); fake when you need state.
  Verify behavior with a mock, verify data with a fake.

### 04-exceptions

The "you can't produce the condition" demo. Tests only; no production change.

- `ThrowsAsync` on the repository mock forces `SaveAsync` into the
  `SqlServerOutOfDiskSpaceException` handler; assert the same instance is
  rethrown. Sibling test for the generic `Exception` handler.
- `FixDiskSpaceProblem()` stays private and is never verified (that needed
  the partial mock, which is gone).
- Speaker line this demo exists for: "the only non-mock way to test this is a
  utility that starts SQL Server and fills the disk with garbage."

### 05-verification

The "interactions" demo: did it call the right stuff?

- Extract `ILogger` from `Logger` into Common. `PersonManager` takes it
  through the constructor; the inline `new Logger()` calls become
  `_logger.WriteToLog(ex)`.
- Tests: `Verify(..., Times.Once)` on the repository, `Times.Never` on the
  logger for the happy path, and `Verify` with the specific exception in both
  failure tests.

### 06-clock

The "data scenario" demo.

- `TimeProvider` through the constructor replaces `DateTime.UtcNow`. The
  console app passes `TimeProvider.System`.
- Show both test clocks: a Moq mock of `TimeProvider`
  (`Setup(x => x.GetUtcNow())`), and `FakeTimeProvider` from
  `Microsoft.Extensions.TimeProvider.Testing` with `Advance()`.
- `SaveAsync_WhenPersonExists_DoesNotChangeCreateDate` becomes
  `..._UpdatesLastModifiedButNotCreateDate`, and
  `SaveAsync_WhenPersonIsNew_PopulatesCreateDateAndLastModified` becomes
  `..._SetsCreateDateAndLastModifiedToNow`, now that the clock can be pinned
  and the tests can check exact values on the `Person` and the saved entity.

### 07-validation

The "not my circus, not my monkeys" demo, and the talk's one live moment.

- `before` adds `PersonValidator : IValidatorStrategy<Person>` with
  `PersonValidatorFixture` (real `Person` objects, no mocks), and
  `PersonManagerMockUtilityFixture.cs`: the same tests as
  `PersonManagerFixture.cs` with `MockUtility.CreateInstance<PersonManager>()`.
- `after`: `PersonManager` takes `IValidatorStrategy<Person>` and refuses to
  save (throws `InvalidObjectException`) when it returns false.
- Show the gotcha on purpose: adding a `bool` dependency to an auto-mocked
  class makes the save tests fail because a Loose mock returns `false`. Fix
  with a `Returns(true)` default in each fixture. In the plain-Moq fixture
  the new constructor parameter fails to compile first, then the same trap
  appears once the mock is passed in. Contrast with
  `FakeValidatorStrategy<T>` from `Benday.EfCore.Testing`, which defaults to
  `true`.

At the end of 07 there is no `DateTime.UtcNow` anywhere in Business and
`PersonManager` creates none of its own dependencies except the adapter.

### refactoring (standalone)

The 2014 Demo 1 used Fakes shims to replace `new PersonDataAccess()` with no
production change. The 2026 version makes the smallest production change
that creates a seam, then mocks through it.

- `before`: the inherited app on raw EF Core, no tests.
- `after`: `protected virtual PersonDataAccess CreateDataAccess()` and
  `protected virtual DateTime GetUtcNow()` seams in `PersonManager`,
  `virtual` members on `PersonDataAccess`, and a test that builds
  `new Mock<PersonManager> { CallBase = true }` and overrides the seams with
  `Protected().Setup(...)`.
- It stays in plain Moq, string-based `Protected()` calls included. After the
  talk, once `Ambient<T>` ships in `Benday.Common.Refactoring`, add a second
  test here showing the same thing with `Ambient<PersonDataAccess>.Replace(...)`
  and no `virtual` at all; keep both so the two techniques can be compared.
- Do not copy its techniques into `01`-`07`.

### extras/setup-tax (standalone)

`PersonRegistrationService` with six constructor dependencies and two
fixtures holding the identical test: `PersonRegistrationServiceMoqOnlyFixture`
(six `Mock<T>` fields) and `PersonRegistrationServiceFixture` (`MockUtility`).
It is the code on the deck's "Annoyance #1: It Gets Wordy" and "MockUtility:
Name Only What You Need" slides, and it's the demo that introduces
`MockUtility`.

## Library dependencies

`Benday.Common.Testing` 4.0.0 is everything the demo needs: `MockUtility`,
`MockCreationResult<T>`, and abstract-class auto-mocking for `TimeProvider`.
There are no `TODO(library)` markers.

The feature release planned in
`/Users/benday/code/benday-inc/Benday.Common/TESTING-TODO.md` (verify
wrappers, argument capture, `SetupForAnyArgs`, `ShouldThrowAsync`) was
dropped before the talk (decided 2026-11-06): plain Moq already covers each
one (`Verify` takes a failure message, `It.Is` and `Invocations` cover
capture, `It.IsAny` is what the audience can use anywhere), and the real
"tests get shorter" payoff is `MockUtility`, which already ships. Use
xUnit's `Assert.ThrowsAsync` for async exceptions. Revisit after the talk,
alongside `CreatePartial<T>` and `Ambient<T>`/`[Seam]` (the new
`Benday.Common.Refactoring` package, which would add a second technique to
the `refactoring/` demo).

Never add a `ProjectReference` to the Benday.Common repo from here; this
repo consumes published packages only (a local NuGet feed is fine during
development: `copy-packages-to-local-nuget.ps1` in that repo).

## Mapping to the talk and the videos

**The talk (VST08, VSLive Orlando, Nov 17 2026) is organized by testing
problem**, and the demo folders now follow the same order as the deck's
"Five Testing Problems" slide.

| Deck problem | Moq move | Demo |
| --- | --- | --- |
| Infrastructure Dependencies (the database) | `new Mock<T>`, `.Object`, `ReturnsAsync`, `It.IsAny`, `Callback` | 02-data-access |
| When a fake beats a mock (state) | `InMemoryRepository<T>` instead of `Mock<T>` | 03-fake-repository |
| Exception Handling (disk full) | `ThrowsAsync` | 04-exceptions |
| Interactions (did it log?) | `Verify`, `Times.Once` / `Never` | 05-verification |
| Data Scenarios (the clock) | `Setup(...).Returns(now)` | 06-clock |
| Not My Circus (validation) | `Returns(false)`; the Loose `false` trap, live | 07-validation |
| Annoyance #1: it gets wordy | `MockUtility.CreateInstance` | extras/setup-tax |
| When not to mock (the adapter) | Real objects | 01-adapter |

The one live moment is the 07 `bool` gotcha, typed into a `prepare-demo.ps1`
scratch folder, with code-slide backup. The refactoring demo is Q&A material
("how do I do this in a monolith with no DI?"). The deck has a QR code for
the YouTube playlist mid-talk and again at the close. The deck itself lives
outside this repo, at
`/Users/benday/code/benday-inc/misc-videos/videos/mocking-with-moq/vst08-mocking-with-moq.json`.

**The videos are diagnostic:** each episode answers a question someone would
search for, and the demo README opens with that question. The episode order
below is a proposal; Ben hasn't fixed it.

| Demo | Title as a search question |
| --- | --- |
| 01-adapter | How do I unit test code that calls the database? Part 1: should I mock my mapping code? |
| 02-data-access | How do I unit test code that calls the database? Part 2: mock the repository |
| 03-fake-repository | Should I mock my repository, or use an in-memory fake? |
| 04-exceptions | How do I test a catch block I can't trigger? |
| 05-verification | How do I verify a method was called with Moq? |
| 06-clock | How do I unit test code that uses `DateTime.Now`? |
| 07-validation | Why does my Moq mock return false? |
| extras/setup-tax | Why are my Moq test fixtures so wordy? |
| refactoring | How do I unit test code that creates its own dependencies? |

## Conventions for this repo

- Test class per class under test, named `{ClassName}Fixture` (so
  `PersonManagerFixture`, `PersonToEntityAdapterFixture`), deriving from
  `TestClassBase`. Each fixture exposes the instance under test through a
  lazily created `SystemUnderTest` property. This is the convention from the
  2014 and 2017 talks and the one the audience will have seen before.
- **`PersonManagerFixture` is plain Moq in every demo.** It's the file that
  grows from 02 to 07, so it never changes technique. The `MockUtility`
  version of the same tests is `PersonManagerMockUtilityFixture.cs`, and it
  exists only in `07-validation` (`SystemUnderTest` there is a
  `MockCreationResult<T>`, so `SystemUnderTest.Instance` and
  `SystemUnderTest.GetRequiredMock<T>()` read naturally). Both are full,
  compiling files with identical tests, never a commented-out block.
  `extras/setup-tax` predates this and uses the older names
  (`...Fixture` = `MockUtility`, `...MoqOnlyFixture` = plain Moq); those class
  names are on slides, so leave them.
- **`TestConstants`.** Tests use `TestConstants.FirstName` ("TestFirstName"),
  `TestConstants.LastName` ("TestLastName"), `TestConstants.EmailAddress`
  ("test@example.com"), `TestConstants.Notes` ("TestNotes") and
  `TestConstants.CreateDate` (a fixed UTC date a stored record already
  carries) instead of made-up people. Each test project has its own
  `TestConstants.cs`. `PersonManagerFixture` builds its inputs with
  `CreateNewPerson()` (name and email, Id 0, no dates) and
  `CreateExistingEntity()` (person 1 as the "database" stores it), and every
  load asserts `FirstName` and `LastName`.
- Test names are sentences a product owner could nod at:
  `SaveAsync_WhenPersonIsInvalid_DoesNotCallRepository`.
- One assertion idea per test. **Every test body carries `// arrange`,
  `// act` and `// assert` comments**, one blank line between the phases. A
  test with nothing to set up still has an `// arrange` with the object it
  acts on (`var person = CreateNewPerson();`).
- **Nullable reference types are on, and the code trusts them.** No `!`
  anywhere in tests or production code. The `Benday.Common.Testing`
  assertions carry `[NotNull]` (`ShouldNotBeNull` and friends), so after
  `actual.ShouldNotBeNull("...")` flow analysis knows `actual` is not null and
  the next line uses it directly. If a `!` seems necessary, the test is shaped
  wrong (assert not-null first).
- **Fixtures use a primary constructor and the `field` keyword.** A fixture
  with nothing to do at construction is declared
  `public class PersonManagerFixture(ITestOutputHelper output) : TestClassBase(output)`,
  and every lazily created mock or system under test is a property over the
  compiler's backing field: `private Mock<ILogger> Logger => field ??= new();`,
  `private PersonManager SystemUnderTest => field ??= new PersonManager(Repository.Object);`.
  No `private Mock<ILogger>? _logger;` declarations. A getter that has to
  configure the object (07's `Validator`, the MockUtility fixture's
  `SystemUnderTest`) uses a block body with `if (field == null) { field = ...; }`.
  The integration fixtures keep an explicit constructor because its body sets
  the connection string. Applies to every fixture in the repo.
- **No constructor null checks.** Constructors assign their parameters
  (`_repository = repository;`), with no `?? throw new ArgumentNullException`.
  The non-nullable parameter type is the contract and the compiler enforces it
  at every call site in these solutions; a runtime guard against `null!` is
  not something the demos defend against.
- `dotnet test <project>` with no extra switches. `xunit.v3.mtp-v2` passes
  unknown switches such as `--nologo` through to the test host, which then
  exits with code 5 and runs nothing. Run it from inside the stage folder
  (`cd 04-exceptions/after && dotnet test Benday.MoqDemo.UnitTests`): the
  stage's `global.json` holds the `"test": { "runner": ... }` opt-in, and from
  the repo root the .NET 10 SDK errors with "Testing with VSTest target is no
  longer supported".
- **Comment the code for the presenter and the audience.** Every class gets a
  short header comment saying what it is and what it demonstrates; every test
  gets a one-line comment naming the Moq move or idea it shows; problem spots
  in the inherited code are labeled "Problem: ...". Comments explain intent,
  not syntax, never mention demo numbers, and stay identical across demos
  wherever the code is identical, so `before`/`after` diffs show only real
  changes.
- Keep demo code small. Every line on screen costs time; if a method needs
  scrolling it needs splitting.
- Do not "fix" the deliberate ugliness in `01-adapter/before` or
  `refactoring/before`, or the deliberate `bool`-default failure in `07`.
  They are the demo.
- Commit per demo with a message that names the demo and the test that
  proves the change.
