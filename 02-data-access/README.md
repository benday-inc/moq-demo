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
   `PersonManager` its `.Object`, and uses `Setup(...).ReturnsAsync(...)` to
   make the "database" answer.

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
- The unit test project references Moq and `Benday.EfCore.Testing`.

**after** puts the database behind the interface:

- `PersonManager(IAsyncReadableRepository<PersonEntity, int> repository)`.
- `PersonDataAccess` is deleted. Business no longer references DataAccess, so
  a unit test can't reach SQL Server even by accident.
- The console app is now the composition root: it builds
  `new SqlPersonRepository(new PersonDbContext())` and passes it in.
- The logger and the clock are still created inline. Demos 04 and 05 fix
  those.

The tests that prove it, in the new `PersonManagerFixture`:

- `LoadByIdAsync_WhenPersonExists_ReturnsThePerson`: `ReturnsAsync` hands back
  person 1.
- `LoadByIdAsync_WhenPersonDoesNotExist_ReturnsNull`
- `SaveAsync_WhenPersonExists_DoesNotChangeCreateDate`
- `LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson`: uses
  `InMemoryRepository<PersonEntity>` from `Benday.EfCore.Testing` instead of a
  mock. Mocks are for interactions, and fakes are for state.

The integration tests (from `before`) need SQL Server:
`SaveAsync_WhenEntityIsNew_AssignsAnId` and
`GetByIdAsync_AfterSaveAsync_ReturnsTheSavedValues`.
