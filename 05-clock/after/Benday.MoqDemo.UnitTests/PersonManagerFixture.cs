using Benday.Common.Testing;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.Common;
using Benday.MoqDemo.DataAccess;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Moq.Protected;

namespace Benday.MoqDemo.UnitTests;

public class PersonManagerFixture : TestClassBase
{
    public PersonManagerFixture(ITestOutputHelper output) : base(output)
    {
    }

    private Mock<ILogger>? _logger;
    private FakeTimeProvider? _clock;
    private Mock<PersonDataAccess>? _dataAccess;
    private Mock<PersonManager>? _partialMock;

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

    private PersonManager SystemUnderTest => PartialMock.Object;

    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow()
    {
        var expected = Clock.GetUtcNow().UtcDateTime;
        var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

        await SystemUnderTest.SaveAsync(person);

        person.CreateDate.ShouldEqual(expected, "CreateDate should come from the clock");
        person.LastModified.ShouldEqual(expected, "LastModified should come from the clock");
    }

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

    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SavesThroughDataAccess()
    {
        var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

        await SystemUnderTest.SaveAsync(person);

        DataAccess.Verify(x => x.SavePersonAsync(It.IsAny<PersonEntity>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_FixesDiskSpaceProblem()
    {
        DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
            .ThrowsAsync(new SqlServerOutOfDiskSpaceException("No more room!"));

        await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        PartialMock.Protected().Verify("FixDiskSpaceProblem", Times.Once());
    }

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

    [Fact]
    public async Task SaveAsync_WhenSaveFailsForAnotherReason_DoesNotFixDiskSpaceProblem()
    {
        DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
            .ThrowsAsync(new InvalidOperationException("Something else broke."));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        PartialMock.Protected().Verify("FixDiskSpaceProblem", Times.Never());
    }

    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_DoesNotLog()
    {
        await SystemUnderTest.SaveAsync(new Person());

        Logger.Verify(x => x.WriteToLog(It.IsAny<Exception>()), Times.Never);
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
