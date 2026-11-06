using Benday.Common.Interfaces;
using Benday.Common.Testing;
using Benday.EfCore.Testing.Fakes;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.Common;
using Moq;

namespace Benday.MoqDemo.UnitTests;

public class PersonManagerFixture : TestClassBase
{
    public PersonManagerFixture(ITestOutputHelper output) : base(output)
    {
    }

    private MockCreationResult<PersonManager>? _systemUnderTest;

    private MockCreationResult<PersonManager> SystemUnderTest =>
        _systemUnderTest ??= MockUtility.CreateInstance<PersonManager>();

    private Mock<IAsyncReadableRepository<PersonEntity, int>> Repository =>
        SystemUnderTest.GetRequiredMock<IAsyncReadableRepository<PersonEntity, int>>();

    private Mock<ILogger> Logger => SystemUnderTest.GetRequiredMock<ILogger>();

    private Mock<TimeProvider> Clock => SystemUnderTest.GetRequiredMock<TimeProvider>();

    private static readonly DateTimeOffset Now = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow()
    {
        Clock.Setup(x => x.GetUtcNow()).Returns(Now);
        var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

        await SystemUnderTest.Instance.SaveAsync(person);

        person.CreateDate.ShouldEqual(Now.UtcDateTime, "CreateDate should come from the clock");
        person.LastModified.ShouldEqual(Now.UtcDateTime, "LastModified should come from the clock");
    }

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

    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SavesThroughTheRepository()
    {
        await SystemUnderTest.Instance.SaveAsync(new Person());

        Repository.Verify(x => x.SaveAsync(It.IsAny<PersonEntity>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_DoesNotLog()
    {
        await SystemUnderTest.Instance.SaveAsync(new Person());

        Logger.Verify(x => x.WriteToLog(It.IsAny<Exception>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_RethrowsTheException()
    {
        var expected = new SqlServerOutOfDiskSpaceException("No more room!");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.Instance.SaveAsync(new Person()));

        actual.ShouldBeSameAs(expected, "SaveAsync should rethrow the original exception");
    }

    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_LogsTheException()
    {
        var expected = new SqlServerOutOfDiskSpaceException("No more room!");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.Instance.SaveAsync(new Person()));

        Logger.Verify(x => x.WriteToLog(expected), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenSaveFailsForAnotherReason_LogsTheException()
    {
        var expected = new InvalidOperationException("Something else broke.");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.Instance.SaveAsync(new Person()));

        Logger.Verify(x => x.WriteToLog(expected), Times.Once);
    }

    [Fact]
    public async Task LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson()
    {
        var sut = MockUtility.Build<PersonManager>()
            .WithValue<IAsyncReadableRepository<PersonEntity, int>>(new InMemoryRepository<PersonEntity>())
            .Build()
            .Instance;
        var saved = new Person { FirstName = "Grace", Notes = "saved in memory" };
        await sut.SaveAsync(saved);

        var actual = await sut.LoadByIdAsync(saved.Id);

        actual.ShouldNotBeNull("the saved person should load back");
        actual!.Notes.ShouldEqual("saved in memory", "Notes should round-trip through the fake");
    }
}
