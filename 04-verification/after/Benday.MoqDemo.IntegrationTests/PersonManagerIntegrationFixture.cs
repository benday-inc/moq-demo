using Benday.Common.Testing;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.DataAccess;
using Microsoft.Extensions.Configuration;

namespace Benday.MoqDemo.IntegrationTests;

// Integration tests for PersonManager against a real SQL Server (the MoqDemoTests
// database). They prove the behavior end to end, and they show the cost: SQL
// Server has to be running, and every test pays for a real round trip. The unit
// tests cover the same behavior without a database.
public class PersonManagerIntegrationFixture : TestClassBase
{
    public PersonManagerIntegrationFixture(ITestOutputHelper output) : base(output)
    {
        // Point EF Core at the test database and create it if it doesn't exist.
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        PersonDbContext.ConnectionString = config.GetConnectionString("MoqDemoTests")
            ?? throw new InvalidOperationException("Missing connection string 'MoqDemoTests'.");

        using var context = new PersonDbContext();
        context.Database.EnsureCreated();
    }

    private PersonManager? _systemUnderTest;

    private PersonManager SystemUnderTest => _systemUnderTest ??= CreatePersonManager();

    // Real dependencies: SQL Server, the console logger.
    private static PersonManager CreatePersonManager()
    {
        return new PersonManager(new SqlPersonRepository(new PersonDbContext()), new Logger());
    }

    private static Person CreateValidPerson()
    {
        return new Person
        {
            FirstName = TestConstants.FirstName,
            LastName = TestConstants.LastName,
            EmailAddress = TestConstants.EmailAddress
        };
    }

    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_AssignsAnId()
    {
        var person = CreateValidPerson();

        await SystemUnderTest.SaveAsync(person);

        person.Id.ShouldBeGreaterThan(0, "SQL Server should assign the Id");
    }

    [Fact]
    public async Task LoadByIdAsync_AfterSaveAsync_ReturnsTheSavedPerson()
    {
        var saved = CreateValidPerson();
        saved.Notes = "integration";
        await SystemUnderTest.SaveAsync(saved);

        // A second PersonManager, so the load really goes to SQL Server instead
        // of coming back from anything the first one still has in memory.
        var actual = await CreatePersonManager().LoadByIdAsync(saved.Id);

        actual.ShouldNotBeNull("the saved person should load back");
        actual!.Notes.ShouldEqual("integration", "Notes should round-trip through SQL Server");
    }

    [Fact]
    public async Task SaveAsync_WhenPersonExists_DoesNotChangeCreateDate()
    {
        var person = CreateValidPerson();
        await SystemUnderTest.SaveAsync(person);
        var createDate = person.CreateDate;

        person.Notes = "updated";
        await SystemUnderTest.SaveAsync(person);

        var actual = await CreatePersonManager().LoadByIdAsync(person.Id);
        actual.ShouldNotBeNull("the updated person should load back");
        actual!.CreateDate.ShouldEqual(createDate, "CreateDate should not change on an update");
        actual.Notes.ShouldEqual("updated", "Notes should be saved on an update");
    }
}
