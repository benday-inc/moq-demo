using Benday.Common.Testing;
using Benday.MoqDemo.Common;
using Benday.MoqDemo.DataAccess;
using Microsoft.Extensions.Configuration;

namespace Benday.MoqDemo.IntegrationTests;

public class SqlPersonRepositoryFixture : TestClassBase
{
    public SqlPersonRepositoryFixture(ITestOutputHelper output) : base(output)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        PersonDbContext.ConnectionString = config.GetConnectionString("MoqDemoTests")
            ?? throw new InvalidOperationException("Missing connection string 'MoqDemoTests'.");

        using var context = new PersonDbContext();
        context.Database.EnsureCreated();
    }

    private SqlPersonRepository? _systemUnderTest;

    private SqlPersonRepository SystemUnderTest => _systemUnderTest ??= new SqlPersonRepository(new PersonDbContext());

    [Fact]
    public async Task SaveAsync_WhenEntityIsNew_AssignsAnId()
    {
        var entity = new PersonEntity { FirstName = "Ada", LastName = "Lovelace" };

        await SystemUnderTest.SaveAsync(entity);

        entity.Id.ShouldBeGreaterThan(0, "SQL Server should assign the Id");
    }

    [Fact]
    public async Task GetByIdAsync_AfterSaveAsync_ReturnsTheSavedValues()
    {
        var saved = new PersonEntity { FirstName = "Grace", LastName = "Hopper", Notes = "integration" };
        await SystemUnderTest.SaveAsync(saved);
        using var readRepository = new SqlPersonRepository(new PersonDbContext());

        var actual = await readRepository.GetByIdAsync(saved.Id);

        actual.ShouldNotBeNull("the saved person should load back");
        actual!.Notes.ShouldEqual("integration", "Notes should round-trip through SQL Server");
    }
}
