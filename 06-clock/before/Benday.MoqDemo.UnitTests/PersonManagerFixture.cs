using Benday.Common.Interfaces;
using Benday.Common.Testing;
using Benday.EfCore.Testing.Fakes;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.Common;
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
    private PersonManager? _systemUnderTest;

    // One mock per constructor parameter. Mock<T> is the remote control you
    // configure and verify; .Object is the stand-in that PersonManager receives.
    private Mock<IAsyncReadableRepository<PersonEntity, int>> Repository => _repository ??= new();

    private Mock<ILogger> Logger => _logger ??= new();

    // The real PersonManager, built with stand-ins. Add a constructor parameter
    // to PersonManager and this line stops compiling until you add another mock above.
    private PersonManager SystemUnderTest => _systemUnderTest ??=
        new PersonManager(Repository.Object, Logger.Object);

    // A person the app hasn't saved yet: Id is 0 and the dates are unset.
    private static Person CreateNewPerson() => new()
    {
        FirstName = TestConstants.FirstName,
        LastName = TestConstants.LastName,
        EmailAddress = TestConstants.EmailAddress
    };

    // What the "database" hands back for person 1.
    private static PersonEntity CreateExistingEntity() => new()
    {
        Id = 1,
        FirstName = TestConstants.FirstName,
        LastName = TestConstants.LastName,
        EmailAddress = TestConstants.EmailAddress,
        CreateDate = TestConstants.CreateDate,
        LastModified = TestConstants.CreateDate
    };

    // Moq move: Setup(...).ReturnsAsync(...) makes the mock answer a question.
    // Here the "database" hands back person 1, and there is no database.
    [Fact]
    public async Task LoadByIdAsync_WhenPersonExists_ReturnsThePerson()
    {
        // arrange
        Repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateExistingEntity());

        // act
        var actual = await SystemUnderTest.LoadByIdAsync(1);

        // assert
        actual.ShouldNotBeNull("person 1 exists, so it should load");
        actual.Id.ShouldEqual(1, "Id should come from the repository");
        actual.FirstName.ShouldEqual(TestConstants.FirstName, "FirstName should come from the repository");
        actual.LastName.ShouldEqual(TestConstants.LastName, "LastName should come from the repository");
    }

    // The same setup with a different answer: the "database" has no person 2.
    [Fact]
    public async Task LoadByIdAsync_WhenPersonDoesNotExist_ReturnsNull()
    {
        // arrange
        Repository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync((PersonEntity?)null);

        // act
        var actual = await SystemUnderTest.LoadByIdAsync(2);

        // assert
        actual.ShouldBeNull("there is no person 2");
    }

    // Moq move: It.IsAny<T>() matches any argument, and Callback(...) runs code
    // when the mock is called. Together they capture what PersonManager handed
    // the repository, so the test can look at it.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SavesTheFieldsToTheRepository()
    {
        // arrange
        PersonEntity? saved = null;
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>()))
            .Callback<PersonEntity>(entity => saved = entity);
        var person = CreateNewPerson();

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        saved.ShouldNotBeNull("SaveAsync should hand the repository an entity");
        saved.FirstName.ShouldEqual(TestConstants.FirstName, "FirstName should be saved");
        saved.LastName.ShouldEqual(TestConstants.LastName, "LastName should be saved");
        saved.EmailAddress.ShouldEqual(TestConstants.EmailAddress, "EmailAddress should be saved");
    }

    // The database assigns the Id. Here the Callback plays the database: it
    // stamps the entity, and PersonManager should copy that Id back onto the Person.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_AssignsTheIdFromTheRepository()
    {
        // arrange
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>()))
            .Callback<PersonEntity>(entity => entity.Id = 42);
        var person = CreateNewPerson();

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        person.Id.ShouldEqual(42, "Id should come back from the repository");
    }

    // A new person gets both dates stamped, on the Person and on the entity that
    // goes to the repository. The clock is still DateTime.UtcNow, so the test can
    // only check that the dates were set, not what they were set to.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_PopulatesCreateDateAndLastModified()
    {
        // arrange
        PersonEntity? saved = null;
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>()))
            .Callback<PersonEntity>(entity => saved = entity);
        var person = CreateNewPerson();

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        person.CreateDate.ShouldNotEqual(default, "CreateDate should be set on the person");
        person.LastModified.ShouldNotEqual(default, "LastModified should be set on the person");
        saved.ShouldNotBeNull("SaveAsync should hand the repository an entity");
        saved.CreateDate.ShouldEqual(person.CreateDate, "CreateDate should be saved");
        saved.LastModified.ShouldEqual(person.LastModified, "LastModified should be saved");
    }

    // An update loads the stored record first. The mock plays the database and
    // hands back person 1 with the CreateDate it was stored with. The caller's
    // copy doesn't carry one, and the stored value has to win.
    [Fact]
    public async Task SaveAsync_WhenPersonExists_DoesNotChangeCreateDate()
    {
        // arrange
        PersonEntity? saved = null;
        Repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateExistingEntity());
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>()))
            .Callback<PersonEntity>(entity => saved = entity);
        var person = CreateNewPerson();
        person.Id = 1;
        person.Notes = TestConstants.Notes;

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        person.CreateDate.ShouldEqual(TestConstants.CreateDate, "CreateDate should come from the stored record");
        saved.ShouldNotBeNull("SaveAsync should hand the repository an entity");
        saved.CreateDate.ShouldEqual(TestConstants.CreateDate, "CreateDate should not change on an update");
        saved.Notes.ShouldEqual(TestConstants.Notes, "Notes should be saved on an update");
    }

    // Saving a person whose Id the database doesn't know is a bug in the caller.
    // ReturnsAsync(null) makes the "database" say it has never heard of them.
    [Fact]
    public async Task SaveAsync_WhenPersonDoesNotExist_Throws()
    {
        // arrange
        Repository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync((PersonEntity?)null);
        var person = CreateNewPerson();
        person.Id = 2;

        // act
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.SaveAsync(person));

        // assert
        actual.Message.ShouldEqual("Unknown person.", "the message should say why the save failed");
    }

    // A fake instead of a mock: InMemoryRepository really stores what you save,
    // so a save followed by a load works with no scripting. A mock answers the
    // questions you script; a fake keeps state.
    [Fact]
    public async Task LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson()
    {
        // arrange
        var sut = new PersonManager(new InMemoryRepository<PersonEntity>(), Logger.Object);
        var saved = CreateNewPerson();
        saved.Notes = TestConstants.Notes;
        await sut.SaveAsync(saved);

        // act
        var actual = await sut.LoadByIdAsync(saved.Id);

        // assert
        actual.ShouldNotBeNull("the saved person should load back");
        actual.Id.ShouldEqual(saved.Id, "the fake should assign an Id and hand it back");
        actual.FirstName.ShouldEqual(TestConstants.FirstName, "FirstName should round-trip through the fake");
        actual.LastName.ShouldEqual(TestConstants.LastName, "LastName should round-trip through the fake");
        actual.Notes.ShouldEqual(TestConstants.Notes, "Notes should round-trip through the fake");
    }

    // State across calls is where a fake earns its keep: the second save has to
    // update the stored record, not add a second one. Items is the fake's "table".
    [Fact]
    public async Task SaveAsync_WhenCalledTwice_UpdatesTheStoredPerson()
    {
        // arrange
        var repository = new InMemoryRepository<PersonEntity>();
        var sut = new PersonManager(repository, Logger.Object);
        var person = CreateNewPerson();
        await sut.SaveAsync(person);
        var createDate = person.CreateDate;
        person.Notes = TestConstants.Notes;

        // act
        await sut.SaveAsync(person);

        // assert
        var stored = repository.Items.ShouldHaveSingleElement("two saves of one person should leave one record");
        stored.Notes.ShouldEqual(TestConstants.Notes, "the update should be stored");
        stored.CreateDate.ShouldEqual(createDate, "CreateDate should not change on an update");
    }

    // Moq move: ThrowsAsync produces a failure you can't produce for real.
    // This one line replaces "start SQL Server and fill the disk with garbage."
    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_RethrowsTheException()
    {
        // arrange
        var expected = new SqlServerOutOfDiskSpaceException("No more room!");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        // act
        var actual = await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.SaveAsync(CreateNewPerson()));

        // assert
        actual.ShouldBeSameAs(expected, "SaveAsync should rethrow the original exception");
    }

    // The generic catch block gets the same treatment with a different exception.
    [Fact]
    public async Task SaveAsync_WhenSaveFailsForAnotherReason_RethrowsTheException()
    {
        // arrange
        var expected = new InvalidOperationException("Something else broke.");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        // act
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.SaveAsync(CreateNewPerson()));

        // assert
        actual.ShouldBeSameAs(expected, "SaveAsync should rethrow the original exception");
    }

    // Moq move: Verify(..., Times.Once) checks the call happened. This is what makes a stub a mock.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SavesThroughTheRepository()
    {
        // arrange
        var person = CreateNewPerson();

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        Repository.Verify(x => x.SaveAsync(It.IsAny<PersonEntity>()), Times.Once);
    }

    // Moq move: Times.Never checks something did NOT happen.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_DoesNotLog()
    {
        // arrange
        var person = CreateNewPerson();

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        Logger.Verify(x => x.WriteToLog(It.IsAny<Exception>()), Times.Never);
    }

    // Verify with a specific argument: the exception that was thrown is the one that got logged.
    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_LogsTheException()
    {
        // arrange
        var expected = new SqlServerOutOfDiskSpaceException("No more room!");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        // act
        await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.SaveAsync(CreateNewPerson()));

        // assert
        Logger.Verify(x => x.WriteToLog(expected), Times.Once);
    }

    // The generic catch block logs too.
    [Fact]
    public async Task SaveAsync_WhenSaveFailsForAnotherReason_LogsTheException()
    {
        // arrange
        var expected = new InvalidOperationException("Something else broke.");
        Repository.Setup(x => x.SaveAsync(It.IsAny<PersonEntity>())).ThrowsAsync(expected);

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.SaveAsync(CreateNewPerson()));

        // assert
        Logger.Verify(x => x.WriteToLog(expected), Times.Once);
    }
}
