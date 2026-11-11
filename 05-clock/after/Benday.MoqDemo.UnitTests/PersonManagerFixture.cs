using Benday.Common.Interfaces;
using Benday.Common.Testing;
using Benday.EfCore.Testing.Fakes;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.Common;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Benday.MoqDemo.UnitTests;

// Tests for PersonManager, written with plain Moq: every mock is created by
// hand and passed to the constructor by hand. Nothing here touches SQL Server.
public class PersonManagerFixture : TestClassBase
{
    public PersonManagerFixture(ITestOutputHelper output) : base(output)
    {
    }

    private Mock<IAsyncReadableRepository<PersonEntity, int>>? _repository;
    private Mock<ILogger>? _logger;
    private Mock<TimeProvider>? _clock;
    private PersonManager? _systemUnderTest;

    // One mock per constructor parameter. Mock<T> is the remote control you
    // configure and verify; .Object is the fake that PersonManager receives.
    private Mock<IAsyncReadableRepository<PersonEntity, int>> Repository => _repository ??= new();

    private Mock<ILogger> Logger => _logger ??= new();

    // TimeProvider is an abstract class, and its GetUtcNow() is virtual, so Moq can fake it.
    private Mock<TimeProvider> Clock => _clock ??= new();

    // The real PersonManager, built with stand-ins. Add a constructor parameter
    // to PersonManager and this line stops compiling until you add another mock above.
    private PersonManager SystemUnderTest => _systemUnderTest ??=
        new PersonManager(Repository.Object, Logger.Object, Clock.Object);

    private static readonly DateTimeOffset Now = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);

    // Moq move: Setup(...).ReturnsAsync(...) makes the mock answer a question.
    // Here the "database" hands back person 1, and there is no database.
    [Fact]
    public async Task LoadByIdAsync_WhenPersonExists_ReturnsThePerson()
    {
        var entity = new PersonEntity { Id = 1, FirstName = TestConstants.FirstName };
        Repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(entity);

        var actual = await SystemUnderTest.LoadByIdAsync(1);

        actual.ShouldNotBeNull("person 1 exists, so it should load");
        actual!.FirstName.ShouldEqual(TestConstants.FirstName, "FirstName should come from the repository");
    }

    // The same setup with a different answer: the "database" has no person 2.
    [Fact]
    public async Task LoadByIdAsync_WhenPersonDoesNotExist_ReturnsNull()
    {
        Repository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync((PersonEntity?)null);

        var actual = await SystemUnderTest.LoadByIdAsync(2);

        actual.ShouldBeNull("there is no person 2");
    }

    // An update loads the person before saving it, so the mock hands one back.
    // With the clock pinned, the test can check LastModified too.
    [Fact]
    public async Task SaveAsync_WhenPersonExists_UpdatesLastModifiedButNotCreateDate()
    {
        var createDate = Now.UtcDateTime.AddDays(-1);
        var person = new Person { Id = 1, CreateDate = createDate };
        Repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new PersonEntity { Id = 1 });
        Clock.Setup(x => x.GetUtcNow()).Returns(Now);

        await SystemUnderTest.SaveAsync(person);

        person.CreateDate.ShouldEqual(createDate, "CreateDate should not change on an update");
        person.LastModified.ShouldEqual(Now.UtcDateTime, "LastModified should move to now");
    }

    // A fake instead of a mock: InMemoryRepository really stores what you save,
    // so a save followed by a load works. Mocks for interactions, fakes for state.
    [Fact]
    public async Task LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson()
    {
        var sut = new PersonManager(new InMemoryRepository<PersonEntity>(), Logger.Object, Clock.Object);
        var saved = new Person { FirstName = TestConstants.FirstName, Notes = "saved in memory" };
        await sut.SaveAsync(saved);

        var actual = await sut.LoadByIdAsync(saved.Id);

        actual.ShouldNotBeNull("the saved person should load back");
        actual!.Notes.ShouldEqual("saved in memory", "Notes should round-trip through the fake");
    }

    // Moq move: ThrowsAsync produces a failure you can't produce for real.
    // This one line replaces "start SQL Server and fill the disk with garbage."
    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_RethrowsTheException()
    {
        var expected = new SqlServerOutOfDiskSpaceException("No more room!");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        actual.ShouldBeSameAs(expected, "SaveAsync should rethrow the original exception");
    }

    // The generic catch block gets the same treatment with a different exception.
    [Fact]
    public async Task SaveAsync_WhenSaveFailsForAnotherReason_RethrowsTheException()
    {
        var expected = new InvalidOperationException("Something else broke.");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        actual.ShouldBeSameAs(expected, "SaveAsync should rethrow the original exception");
    }

    // Moq move: Verify(..., Times.Once) checks the call happened. This is what makes a stub a mock.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SavesThroughTheRepository()
    {
        await SystemUnderTest.SaveAsync(new Person());

        Repository.Verify(x => x.SaveAsync(It.IsAny<PersonEntity>()), Times.Once);
    }

    // Moq move: Times.Never checks something did NOT happen.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_DoesNotLog()
    {
        await SystemUnderTest.SaveAsync(new Person());

        Logger.Verify(x => x.WriteToLog(It.IsAny<Exception>()), Times.Never);
    }

    // Verify with a specific argument: the exception that was thrown is the one that got logged.
    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_LogsTheException()
    {
        var expected = new SqlServerOutOfDiskSpaceException("No more room!");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        Logger.Verify(x => x.WriteToLog(expected), Times.Once);
    }

    // The generic catch block logs too.
    [Fact]
    public async Task SaveAsync_WhenSaveFailsForAnotherReason_LogsTheException()
    {
        var expected = new InvalidOperationException("Something else broke.");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        Logger.Verify(x => x.WriteToLog(expected), Times.Once);
    }

    // Moq move: Setup(...).Returns(...) pins the clock, so the test knows what "now" is.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow()
    {
        Clock.Setup(x => x.GetUtcNow()).Returns(Now);
        var person = new Person { FirstName = TestConstants.FirstName, LastName = TestConstants.LastName };

        await SystemUnderTest.SaveAsync(person);

        person.CreateDate.ShouldEqual(Now.UtcDateTime, "CreateDate should come from the clock");
        person.LastModified.ShouldEqual(Now.UtcDateTime, "LastModified should come from the clock");
    }

    // FakeTimeProvider: Microsoft's ready-made fake clock. Unlike a mock, it can
    // move: Advance() makes time pass between the two saves.
    [Fact]
    public async Task SaveAsync_WhenTimeMovesForward_UpdatesLastModified()
    {
        var clock = new FakeTimeProvider(Now);
        var sut = new PersonManager(new InMemoryRepository<PersonEntity>(), Logger.Object, clock);
        var person = new Person { FirstName = TestConstants.FirstName };
        await sut.SaveAsync(person);

        clock.Advance(TimeSpan.FromHours(1));
        await sut.SaveAsync(person);

        person.LastModified.ShouldEqual(Now.UtcDateTime.AddHours(1), "LastModified should follow the clock");
    }
}
