# 02: Mock the database

**The question this answers:** How do I unit test code that calls the database? Part 2: mock the repository.

## Goal

Test `PersonManager` without SQL Server. This is the "infrastructure
dependency" reason to mock: the real dependency is slow, needs a server, and
fails for reasons that have nothing to do with your code.

## How

Two moves, and they're the foundation for every demo after this one:

1. **Depend on an interface.** `PersonManager` stops creating
   `PersonDataAccess` with `new` and takes
   `IAsyncReadableRepository<PersonEntity, int>` through its constructor.
2. **Pass a mock.** The test creates a `Mock<T>` of that interface, hands
   `PersonManager` its `.Object`, and scripts the "database" with
   `Setup(...).ReturnsAsync(...)`. For the save path, `It.IsAny<T>()` and
   `Callback(...)` let the test see what `PersonManager` handed the repository,
   and play the database's part (assign the Id).

The real repository, `SqlPersonRepository`, still gets tested, against a real
SQL Server, in the integration test project.

## What changes

**before** is demo 01's `after`, plus setup the video skips:

- A new Common project holds `PersonEntity`, so Business can use it without
  referencing DataAccess.
- `SqlPersonRepository : EfCoreCrudRepositoryBase<PersonEntity, PersonDbContext>`
  exists in DataAccess but isn't used yet.
- A new `Benday.MoqDemo.IntegrationTests` project round-trips it against SQL
  Server, in a separate `MoqDemoTests` database.
- `PersonManagerIntegrationFixture`, in that same project, drives the
  inherited `PersonManager` against SQL Server. It passes today, so it is the
  safety net for the refactor, and it shows the cost the unit tests avoid:
  SQL Server has to be running, and every test pays for a real round trip.
- The unit test project references Moq. Nothing else: the in-memory fake is
  demo 03's subject.

**after** puts the database behind the interface:

- `PersonManager(IAsyncReadableRepository<PersonEntity, int> repository)`.
  Adding the constructor breaks the build in exactly two places, the console
  app and the integration fixture. That's the compiler finding the
  composition root for you; fix both with
  `new SqlPersonRepository(new PersonDbContext())` before touching anything
  else. No transitional parameterless constructor, no `null!`.
- The first unit test then fails at runtime, not compile time: `PersonManager`
  still calls `new PersonDataAccess()` inside its methods and tries to reach
  SQL Server. Each method is refactored to `_repository` with a test watching.
- `PersonDataAccess` is deleted. Business no longer references DataAccess, so
  a unit test can't reach SQL Server even by accident.
- One behavior fix surfaced by the tests: on an update, the stored record's
  `CreateDate` wins over whatever the caller's `Person` says. Before, the
  adapter copied the caller's `CreateDate` (often `default`) over the real one.
- The logger and the clock are still created inline. Demos 05 and 06 fix
  those, which is why the date tests here can only check that the dates were
  set, not what they were set to.

The tests that prove it, in the new `PersonManagerFixture` (plain Moq, one
mock, every test with `// arrange`, `// act`, `// assert`):

- `LoadByIdAsync_WhenPersonExists_ReturnsThePerson`: `ReturnsAsync` hands back
  person 1; first and last name come through.
- `LoadByIdAsync_WhenPersonDoesNotExist_ReturnsNull`
- `SaveAsync_WhenPersonIsNew_SavesTheFieldsToTheRepository`: `It.IsAny` plus
  `Callback` captures the entity handed to `SaveAsync`.
- `SaveAsync_WhenPersonIsNew_AssignsTheIdFromTheRepository`: the `Callback`
  plays the database and stamps `Id = 42`.
- `SaveAsync_WhenPersonIsNew_PopulatesCreateDateAndLastModified`: on the
  `Person` and on the saved entity.
- `SaveAsync_WhenPersonExists_DoesNotChangeCreateDate`: the mock hands back a
  stored record with a known `CreateDate`; the caller's copy has none; the
  stored one wins on both the `Person` and the saved entity.
- `SaveAsync_WhenPersonDoesNotExist_Throws`: `ReturnsAsync(null)` on the load,
  `InvalidOperationException` out.

Nullable note: the `Benday.Common.Testing` assertions carry `[NotNull]`, so
after `actual.ShouldNotBeNull(...)` the compiler knows `actual` is not null and
no `!` is needed.

The integration tests (from `before`) need SQL Server. `SqlPersonRepositoryFixture`
covers the real repository: `SaveAsync_WhenEntityIsNew_AssignsAnId` and
`GetByIdAsync_AfterSaveAsync_ReturnsTheSavedValues`.
`PersonManagerIntegrationFixture` covers `PersonManager` end to end:
`SaveAsync_WhenPersonIsNew_AssignsAnId`,
`SaveAsync_WhenPersonIsNew_PopulatesCreateDateAndLastModified`,
`LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson` and
`SaveAsync_WhenPersonExists_DoesNotChangeCreateDate`. Two of those have the
same names as unit tests in `PersonManagerFixture`: same behavior, one with a
database and one without. The only line that changes in the integration
fixture between `before` and `after` is the constructor call, from
`new PersonManager()` to
`new PersonManager(new SqlPersonRepository(new PersonDbContext()))`, the same
change the console app makes.
