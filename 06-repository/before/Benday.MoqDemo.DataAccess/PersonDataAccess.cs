using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.DataAccess;

// Talks to SQL Server through EF Core. The methods are virtual because Moq can
// only fake virtual members of a concrete class; that's what lets the tests use
// a Mock<PersonDataAccess>.
public class PersonDataAccess
{
    public virtual async Task<PersonEntity?> LoadPersonAsync(int id)
    {
        using var context = new PersonDbContext();

        return await context.Persons.FindAsync(id);
    }

    public virtual async Task SavePersonAsync(PersonEntity saveThis)
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
