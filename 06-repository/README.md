# 06: Repository and the setup tax

Through stage 05, every test pays a setup tax: a hand-built partial mock,
string-based `Protected()` calls, and a mock of a concrete class with
`virtual` members. That's what it costs to test code that news up its own
data access.

**before** is stage 05's `after`, plus setup the video skips:

- `PersonEntity` moves to Common, which now references `Benday.EfCore`.
- `SqlPersonRepository : EfCoreCrudRepositoryBase<PersonEntity, PersonDbContext>`
  exists in DataAccess but isn't used yet.
- A new `Benday.MoqDemo.IntegrationTests` project round-trips it against SQL
  Server, in a separate `MoqDemoTests` database. It's the only project that
  needs Docker.
- The unit test project references `Benday.EfCore.Testing`.

**after** passes every dependency through the constructor:

- `PersonManager(IAsyncReadableRepository<PersonEntity, int> repository, ILogger logger, TimeProvider timeProvider)`.
  It uses `Benday.Common.Interfaces`' repository interface directly, so the
  in-memory fake fits without a wrapper.
- `CreateDataAccess()` and `PersonDataAccess` are deleted.
  `FixDiskSpaceProblem()` is `private` again. Business no longer references
  DataAccess, so a unit test can't reach SQL Server even by accident.
- The console app builds `new SqlPersonRepository(new PersonDbContext())`.
- `PersonManagerFixture` becomes `MockUtility.CreateInstance<PersonManager>()`.
  Each test configures only the mocks it cares about with
  `GetRequiredMock<T>()`. `TimeProvider` is an abstract class, and
  `Benday.Common.Testing` 4.0.0 auto-mocks it too.

The tests that prove it:

- The 7 behavior tests from stage 05 still pass, rewritten against the
  repository mock. The two `FixDiskSpaceProblem` tests are gone: it's private
  now, so the disk-space tests check the rethrow and the log instead.
- `LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson` uses
  `InMemoryRepository<PersonEntity>` from `Benday.EfCore.Testing` through
  `MockUtility.Build<PersonManager>().WithValue(...)`. Mocks are for
  interactions, and fakes are for state.
- `SqlPersonRepositoryFixture` (integration): `SaveAsync_WhenEntityIsNew_AssignsAnId`
  and `GetByIdAsync_AfterSaveAsync_ReturnsTheSavedValues`.

Here's the stage 05 version of the disk-space test, with its share of the
fixture's setup:

```csharp
private Mock<ILogger> Logger => _logger ??= new Mock<ILogger>();

private FakeTimeProvider Clock => _clock ??=
    new FakeTimeProvider(new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero));

private Mock<PersonDataAccess> DataAccess => _dataAccess ??= new Mock<PersonDataAccess>();

private Mock<PersonManager> PartialMock
{
    get
    {
        if (_partialMock == null)
        {
            _partialMock = new Mock<PersonManager>(Logger.Object, Clock) { CallBase = true };

            _partialMock.Protected()
                .Setup<PersonDataAccess>("CreateDataAccess")
                .Returns(DataAccess.Object);
        }

        return _partialMock;
    }
}

[Fact]
public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_LogsTheException()
{
    var expected = new SqlServerOutOfDiskSpaceException("No more room!");
    DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
        .ThrowsAsync(expected);

    await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
        () => SystemUnderTest.SaveAsync(new Person()));

    Logger.Verify(x => x.WriteToLog(expected), Times.Once);
}
```

And stage 06's:

```csharp
private MockCreationResult<PersonManager> SystemUnderTest =>
    _systemUnderTest ??= MockUtility.CreateInstance<PersonManager>();

[Fact]
public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_LogsTheException()
{
    var expected = new SqlServerOutOfDiskSpaceException("No more room!");
    Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

    await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
        () => SystemUnderTest.Instance.SaveAsync(new Person()));

    Logger.Verify(x => x.WriteToLog(expected), Times.Once);
}
```
