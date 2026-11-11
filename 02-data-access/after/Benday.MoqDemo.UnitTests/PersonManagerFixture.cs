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
    private PersonManager? _systemUnderTest;

    // One mock per constructor parameter. Mock<T> is the remote control you
    // configure and verify; .Object is the fake that PersonManager receives.
    private Mock<IAsyncReadableRepository<PersonEntity, int>> Repository => _repository ??= new();

    // The real PersonManager, built with the stand-in.
    private PersonManager SystemUnderTest => _systemUnderTest ??= new PersonManager(Repository.Object);

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
    [Fact]
    public async Task SaveAsync_WhenPersonExists_DoesNotChangeCreateDate()
    {
        var createDate = new DateTime(2026, 11, 16, 9, 0, 0, DateTimeKind.Utc);
        var person = new Person { Id = 1, CreateDate = createDate };
        Repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new PersonEntity { Id = 1 });

        await SystemUnderTest.SaveAsync(person);

        person.CreateDate.ShouldEqual(createDate, "CreateDate should not change on an update");
    }

    // A fake instead of a mock: InMemoryRepository really stores what you save,
    // so a save followed by a load works. Mocks for interactions, fakes for state.
    [Fact]
    public async Task LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson()
    {
        var sut = new PersonManager(new InMemoryRepository<PersonEntity>());
        var saved = new Person { FirstName = TestConstants.FirstName, Notes = "saved in memory" };
        await sut.SaveAsync(saved);

        var actual = await sut.LoadByIdAsync(saved.Id);

        actual.ShouldNotBeNull("the saved person should load back");
        actual!.Notes.ShouldEqual("saved in memory", "Notes should round-trip through the fake");
    }
}
