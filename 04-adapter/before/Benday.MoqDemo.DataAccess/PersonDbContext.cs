using Microsoft.EntityFrameworkCore;

namespace Benday.MoqDemo.DataAccess;

public class PersonDbContext : DbContext
{
    // Set once at startup by the console app. Everything that news up a
    // PersonDbContext depends on this having been set first.
    public static string ConnectionString { get; set; } = string.Empty;

    public DbSet<PersonEntity> Persons => Set<PersonEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(ConnectionString);
    }
}
