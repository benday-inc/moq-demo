using Benday.Common.Testing;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.Common;
using Benday.MoqDemo.DataAccess;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Moq.Protected;

namespace Benday.MoqDemo.UnitTests;

// Tests for PersonManager. PersonManager creates its own data access, so these
// tests use a partial mock: a Mock<PersonManager> that runs the real code
// (CallBase = true) except for the protected virtual seams the tests override.
public class PersonManagerFixture : TestClassBase
{
    public PersonManagerFixture(ITestOutputHelper output) : base(output)
    {
    }

    private Mock<ILogger>? _logger;
    private FakeTimeProvider? _clock;
    private Mock<PersonDataAccess>? _dataAccess;
    private Mock<PersonManager>? _partialMock;

    // A plain mock of the interface, passed to PersonManager's constructor.
    private Mock<ILogger> Logger => _logger ??= new Mock<ILogger>();

    // FakeTimeProvider: a ready-made fake clock from Microsoft, fixed at a known
    // time. A test can also move it forward with Advance().
    private FakeTimeProvider Clock => _clock ??=
        new FakeTimeProvider(new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero));

    // A mock of a concrete class. It works because PersonDataAccess's methods are virtual.
    private Mock<PersonDataAccess> DataAccess => _dataAccess ??= new Mock<PersonDataAccess>();

    private Mock<PersonManager> PartialMock
    {
        get
        {
            if (_partialMock == null)
            {
                // CallBase = true: run PersonManager's real code unless a member is overridden.
                _partialMock = new Mock<PersonManager>(Logger.Object, Clock) { CallBase = true };

                // Override the protected CreateDataAccess() seam by name (it's protected, so a
                // lambda can't reach it). Now every PersonDataAccess the real code asks for is the mock.
                _partialMock.Protected()
                    .Setup<PersonDataAccess>("CreateDataAccess")
                    .Returns(DataAccess.Object);
            }

            return _partialMock;
        }
    }

    // .Object is the partial mock's PersonManager instance: real code plus overrides.
    private PersonManager SystemUnderTest => PartialMock.Object;

    // The fake clock's time is the expected time.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow()
    {
        var expected = Clock.GetUtcNow().UtcDateTime;
        var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

        await SystemUnderTest.SaveAsync(person);

        person.CreateDate.ShouldEqual(expected, "CreateDate should come from the clock");
        person.LastModified.ShouldEqual(expected, "LastModified should come from the clock");
    }

    // Advance() moves the fake clock forward a day between creating and saving,
    // which a hand-rolled seam can't do.
    [Fact]
    public async Task SaveAsync_WhenPersonExists_UpdatesLastModifiedButNotCreateDate()
    {
        var createDate = Clock.GetUtcNow().UtcDateTime;
        var person = new Person { Id = 1, CreateDate = createDate };
        DataAccess.Setup(x => x.LoadPersonAsync(1)).ReturnsAsync(new PersonEntity { Id = 1 });
        Clock.Advance(TimeSpan.FromDays(1));

        await SystemUnderTest.SaveAsync(person);

        person.CreateDate.ShouldEqual(createDate, "CreateDate should not change on an update");
        person.LastModified.ShouldEqual(createDate.AddDays(1), "LastModified should move to now");
    }

    // The Moq alternative to FakeTimeProvider: GetUtcNow() is virtual, so
    // Mock<TimeProvider> works too. FakeTimeProvider is less setup and can move time.
    [Fact]
    public async Task SaveAsync_WithAMockedTimeProvider_SetsCreateDateToNow()
    {
        var expected = new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);
        var clock = new Mock<TimeProvider>();
        clock.Setup(x => x.GetUtcNow()).Returns(expected);
        var partialMock = new Mock<PersonManager>(Logger.Object, clock.Object) { CallBase = true };
        partialMock.Protected().Setup<PersonDataAccess>("CreateDataAccess").Returns(DataAccess.Object);
        var person = new Person();

        await partialMock.Object.SaveAsync(person);

        person.CreateDate.ShouldEqual(expected.UtcDateTime, "CreateDate should come from the mocked clock");
    }

    // Moq move: Verify(..., Times.Once) checks the real code called the mocked data access exactly once.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SavesThroughDataAccess()
    {
        var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

        await SystemUnderTest.SaveAsync(person);

        DataAccess.Verify(x => x.SavePersonAsync(It.IsAny<PersonEntity>()), Times.Once);
    }

    // Moq move: ThrowsAsync makes the mocked data access fail with the exception
    // nothing else can produce. Protected().Verify then checks the handler ran.
    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_FixesDiskSpaceProblem()
    {
        DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
            .ThrowsAsync(new SqlServerOutOfDiskSpaceException("No more room!"));

        await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        PartialMock.Protected().Verify("FixDiskSpaceProblem", Times.Once());
    }

    // The same instance comes back out: the handler rethrows rather than wrapping it.
    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_RethrowsTheException()
    {
        var expected = new SqlServerOutOfDiskSpaceException("No more room!");
        DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
            .ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        actual.ShouldBeSameAs(expected, "SaveAsync should rethrow the original exception");
    }

    // A different exception lands in the generic handler, so FixDiskSpaceProblem must not run: Times.Never.
    [Fact]
    public async Task SaveAsync_WhenSaveFailsForAnotherReason_DoesNotFixDiskSpaceProblem()
    {
        DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
            .ThrowsAsync(new InvalidOperationException("Something else broke."));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        PartialMock.Protected().Verify("FixDiskSpaceProblem", Times.Never());
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
        DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
            .ThrowsAsync(expected);

        await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        Logger.Verify(x => x.WriteToLog(expected), Times.Once);
    }

    // The generic catch block gets the same treatment with a different exception.
    [Fact]
    public async Task SaveAsync_WhenSaveFailsForAnotherReason_LogsTheException()
    {
        var expected = new InvalidOperationException("Something else broke.");
        DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
            .ThrowsAsync(expected);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        Logger.Verify(x => x.WriteToLog(expected), Times.Once);
    }
}
