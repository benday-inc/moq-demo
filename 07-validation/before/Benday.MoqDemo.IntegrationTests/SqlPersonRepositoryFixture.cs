using Benday.Common.Testing;
using Benday.MoqDemo.Common;
using Benday.MoqDemo.DataAccess;
using Microsoft.Extensions.Configuration;

namespace Benday.MoqDemo.IntegrationTests;

// Integration tests: these really talk to SQL Server (the MoqDemoTests database).
// They're slower and need SQL Server running, which is exactly the cost the unit
// tests avoid by mocking the repository.
public class SqlPersonRepositoryFixture : TestClassBase
{
    public SqlPersonRepositoryFixture(ITestOutputHelper output) : base(output)
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

    private SqlPersonRepository SystemUnderTest => field ??= new SqlPersonRepository(new PersonDbContext());

    [Fact]
    public async Task SaveAsync_WhenEntityIsNew_AssignsAnId()
    {
        // arrange
        var entity = new PersonEntity { FirstName = TestConstants.FirstName, LastName = TestConstants.LastName };

        // act
        await SystemUnderTest.SaveAsync(entity);

        // assert
        entity.Id.ShouldBeGreaterThan(0, "SQL Server should assign the Id");
    }

    [Fact]
    public async Task GetByIdAsync_AfterSaveAsync_ReturnsTheSavedValues()
    {
        // arrange
        var saved = new PersonEntity
        {
            FirstName = TestConstants.FirstName,
            LastName = TestConstants.LastName,
            Notes = "integration"
        };
        await SystemUnderTest.SaveAsync(saved);
        using var readRepository = new SqlPersonRepository(new PersonDbContext());

        // act
        var actual = await readRepository.GetByIdAsync(saved.Id);

        // assert
        actual.ShouldNotBeNull("the saved person should load back");
        actual.FirstName.ShouldEqual(TestConstants.FirstName, "FirstName should round-trip through SQL Server");
        actual.LastName.ShouldEqual(TestConstants.LastName, "LastName should round-trip through SQL Server");
        actual.Notes.ShouldEqual("integration", "Notes should round-trip through SQL Server");
    }
}
