using Benday.Common.Interfaces;
using Benday.Common.Testing;
using Benday.EfCore.ServiceLayers;
using Benday.EfCore.Testing.Fakes;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.Common;
using Moq;

namespace Benday.MoqDemo.UnitTests;

// Tests for PersonManager. MockUtility.CreateInstance<PersonManager>() creates a
// mock for every constructor parameter and builds PersonManager with them, so
// each test configures only the mocks it cares about. PersonManagerMoqOnlyFixture
// has the same tests with the mocks wired by hand.
public class PersonManagerFixture : TestClassBase
{
    public PersonManagerFixture(ITestOutputHelper output) : base(output)
    {
    }

    private MockCreationResult<PersonManager>? _systemUnderTest;

    private MockCreationResult<PersonManager> SystemUnderTest
    {
        get
        {
            if (_systemUnderTest == null)
            {
                _systemUnderTest = MockUtility.CreateInstance<PersonManager>();

                // A Loose mock returns false for bool, so every person would be invalid.
                _systemUnderTest.GetRequiredMock<IValidatorStrategy<Person>>()
                    .Setup(x => x.IsValid(It.IsAny<Person>()))
                    .Returns(true);
            }

            return _systemUnderTest;
        }
    }

    // GetRequiredMock<T>() returns the mock MockUtility created for that constructor
    // parameter, and throws if PersonManager has no such parameter.
    private Mock<IAsyncReadableRepository<PersonEntity, int>> Repository =>
        SystemUnderTest.GetRequiredMock<IAsyncReadableRepository<PersonEntity, int>>();

    private Mock<ILogger> Logger => SystemUnderTest.GetRequiredMock<ILogger>();

    private Mock<IValidatorStrategy<Person>> Validator =>
        SystemUnderTest.GetRequiredMock<IValidatorStrategy<Person>>();

    // TimeProvider is an abstract class. MockUtility (Benday.Common.Testing 4.0+) mocks those too.
    private Mock<TimeProvider> Clock => SystemUnderTest.GetRequiredMock<TimeProvider>();

    private static readonly DateTimeOffset Now = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);

    // Moq move: Setup(...).Returns(...) pins the clock, so the test knows what "now" is.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow()
    {
        Clock.Setup(x => x.GetUtcNow()).Returns(Now);
        var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

        await SystemUnderTest.Instance.SaveAsync(person);

        person.CreateDate.ShouldEqual(Now.UtcDateTime, "CreateDate should come from the clock");
        person.LastModified.ShouldEqual(Now.UtcDateTime, "LastModified should come from the clock");
    }

    // Moq move: ReturnsAsync stubs an async method. Here the "database" hands back person 1.
    [Fact]
    public async Task SaveAsync_WhenPersonExists_UpdatesLastModifiedButNotCreateDate()
    {
        var createDate = Now.UtcDateTime.AddDays(-1);
        var person = new Person { Id = 1, CreateDate = createDate };
        Repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new PersonEntity { Id = 1 });
        Clock.Setup(x => x.GetUtcNow()).Returns(Now);

        await SystemUnderTest.Instance.SaveAsync(person);

        person.CreateDate.ShouldEqual(createDate, "CreateDate should not change on an update");
        person.LastModified.ShouldEqual(Now.UtcDateTime, "LastModified should move to now");
    }

    // Moq move: Verify(..., Times.Once) checks the call happened. This is what makes a stub a mock.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SavesThroughTheRepository()
    {
        await SystemUnderTest.Instance.SaveAsync(new Person());

        Repository.Verify(x => x.SaveAsync(It.IsAny<PersonEntity>()), Times.Once);
    }

    // Returns(false) overrides the fixture's default: the validator says no, so
    // the repository must never be called.
    [Fact]
    public async Task SaveAsync_WhenPersonIsInvalid_DoesNotCallRepository()
    {
        Validator.Setup(x => x.IsValid(It.IsAny<Person>())).Returns(false);

        await Assert.ThrowsAsync<InvalidObjectException>(
            () => SystemUnderTest.Instance.SaveAsync(new Person()));

        Repository.Verify(x => x.SaveAsync(It.IsAny<PersonEntity>()), Times.Never);
    }

    // Moq move: Times.Never checks something did NOT happen.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_DoesNotLog()
    {
        await SystemUnderTest.Instance.SaveAsync(new Person());

        Logger.Verify(x => x.WriteToLog(It.IsAny<Exception>()), Times.Never);
    }

    // Moq move: ThrowsAsync produces a failure you can't produce for real.
    // This one line replaces "start SQL Server and fill the disk with garbage."
    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_RethrowsTheException()
    {
        var expected = new SqlServerOutOfDiskSpaceException("No more room!");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.Instance.SaveAsync(new Person()));

        actual.ShouldBeSameAs(expected, "SaveAsync should rethrow the original exception");
    }

    // Verify with a specific argument: the exception that was thrown is the one that got logged.
    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_LogsTheException()
    {
        var expected = new SqlServerOutOfDiskSpaceException("No more room!");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.Instance.SaveAsync(new Person()));

        Logger.Verify(x => x.WriteToLog(expected), Times.Once);
    }

    // The generic catch block gets the same treatment with a different exception.
    [Fact]
    public async Task SaveAsync_WhenSaveFailsForAnotherReason_LogsTheException()
    {
        var expected = new InvalidOperationException("Something else broke.");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.Instance.SaveAsync(new Person()));

        Logger.Verify(x => x.WriteToLog(expected), Times.Once);
    }

    // Fakes instead of mocks: InMemoryRepository really stores what you save, and
    // FakeValidatorStrategy says "valid" unless told otherwise. A fake carries a
    // sensible default; a mock returns default(T).
    [Fact]
    public async Task LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson()
    {
        var sut = MockUtility.Build<PersonManager>()
            .WithValue<IAsyncReadableRepository<PersonEntity, int>>(new InMemoryRepository<PersonEntity>())
            .WithValue<IValidatorStrategy<Person>>(new FakeValidatorStrategy<Person>())
            .Build()
            .Instance;
        var saved = new Person { FirstName = "Grace", Notes = "saved in memory" };
        await sut.SaveAsync(saved);

        var actual = await sut.LoadByIdAsync(saved.Id);

        actual.ShouldNotBeNull("the saved person should load back");
        actual!.Notes.ShouldEqual("saved in memory", "Notes should round-trip through the fake");
    }
}
