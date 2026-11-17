using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.DataAccess;

// Talks to SQL Server through EF Core. PersonManager creates one of these with
// new, which is why PersonManager is hard to test.
public class PersonDataAccess
{
    public async Task<PersonEntity?> LoadPersonAsync(int id)
    {
        using var context = new PersonDbContext();

        return await context.Persons.FindAsync(id);
    }

    public async Task SavePersonAsync(PersonEntity saveThis)
    {
        using var context = new PersonDbContext();

        if (saveThis.Id == 0)
        {
            context.Persons.Add(saveThis);
        }
        else
        {
            context.Persons.Update(saveThis);
        }

        await context.SaveChangesAsync();
    }
}
