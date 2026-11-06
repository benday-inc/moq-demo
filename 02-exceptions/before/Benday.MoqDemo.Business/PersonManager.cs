using Benday.MoqDemo.DataAccess;

namespace Benday.MoqDemo.Business;

public class PersonManager
{
    public Person CreateNew()
    {
        return new Person();
    }

    public async Task SaveAsync(Person saveThis)
    {
        try
        {
            if (saveThis.Id == 0)
            {
                await SaveNewAsync(saveThis);
            }
            else
            {
                await SaveExistingAsync(saveThis);
            }
        }
        catch (SqlServerOutOfDiskSpaceException ex)
        {
            FixDiskSpaceProblem();
            new Logger().WriteToLog(ex);
            throw;
        }
        catch (Exception ex)
        {
            new Logger().WriteToLog(ex);
            throw;
        }
    }

    // Seam for tests; do not inline.
    protected virtual PersonDataAccess CreateDataAccess() => new PersonDataAccess();

    // Seam for tests; do not inline.
    protected virtual DateTime GetUtcNow() => DateTime.UtcNow;

    private void FixDiskSpaceProblem()
    {
        Console.WriteLine("Hey, somebody should really do something about that database. It's outta disk space.");
    }

    private async Task SaveNewAsync(Person saveThis)
    {
        var dataAccess = CreateDataAccess();

        saveThis.CreateDate = GetUtcNow();
        saveThis.LastModified = GetUtcNow();

        var entity = new PersonEntity();
        CopyToEntity(saveThis, entity);

        await dataAccess.SavePersonAsync(entity);

        saveThis.Id = entity.Id;
    }

    private async Task SaveExistingAsync(Person saveThis)
    {
        var dataAccess = CreateDataAccess();

        var entity = await dataAccess.LoadPersonAsync(saveThis.Id)
            ?? throw new InvalidOperationException("Unknown person.");

        saveThis.LastModified = GetUtcNow();

        CopyToEntity(saveThis, entity);

        await dataAccess.SavePersonAsync(entity);
    }

    private static void CopyToEntity(Person fromValue, PersonEntity toValue)
    {
        toValue.FirstName = fromValue.FirstName;
        toValue.LastName = fromValue.LastName;
        toValue.EmailAddress = fromValue.EmailAddress;
        toValue.Notes = fromValue.Notes;
        toValue.CreateDate = fromValue.CreateDate;
        toValue.LastModified = fromValue.LastModified;
    }

    public async Task<Person?> LoadByIdAsync(int id)
    {
        var dataAccess = CreateDataAccess();

        var entity = await dataAccess.LoadPersonAsync(id);

        if (entity == null)
        {
            return null;
        }

        return new Person
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            EmailAddress = entity.EmailAddress,
            Notes = entity.Notes,
            CreateDate = entity.CreateDate,
            LastModified = entity.LastModified
        };
    }
}
