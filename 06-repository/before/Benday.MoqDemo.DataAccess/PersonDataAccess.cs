using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.DataAccess;

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
