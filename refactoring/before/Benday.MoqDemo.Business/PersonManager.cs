using Benday.MoqDemo.DataAccess;

namespace Benday.MoqDemo.Business;

// The system under test. This is the inherited, untested version: it creates
// its own dependencies (new PersonDataAccess(), new Logger(), DateTime.UtcNow)
// right where it uses them, so a test can't swap any of them out.
// How would you test SaveAsync without a running SQL Server?
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
        // Nothing in the app ever throws this, so no ordinary test can reach this
        // handler. A mock that throws it can.
        catch (SqlServerOutOfDiskSpaceException ex)
        {
            FixDiskSpaceProblem();
            // Problem: the logger is created right here, so a test can't check that
            // anything was logged.
            new Logger().WriteToLog(ex);
            throw;
        }
        // Any other failure: log it and rethrow.
        catch (Exception ex)
        {
            new Logger().WriteToLog(ex);
            throw;
        }
    }

    // The "very specific handling" for a full disk.
    private void FixDiskSpaceProblem()
    {
        Console.WriteLine("Hey, somebody should really do something about that database. It's outta disk space.");
    }

    private async Task SaveNewAsync(Person saveThis)
    {
        // Problem: PersonManager creates its own data access, so every call goes to
        // SQL Server and a test has no way to substitute anything.
        var dataAccess = new PersonDataAccess();

        // Problem: the real clock. A test can't know what time this will be.
        saveThis.CreateDate = DateTime.UtcNow;
        saveThis.LastModified = DateTime.UtcNow;

        var entity = new PersonEntity();
        CopyToEntity(saveThis, entity);

        await dataAccess.SavePersonAsync(entity);

        saveThis.Id = entity.Id;
    }

    private async Task SaveExistingAsync(Person saveThis)
    {
        var dataAccess = new PersonDataAccess();

        var entity = await dataAccess.LoadPersonAsync(saveThis.Id)
            ?? throw new InvalidOperationException("Unknown person.");

        saveThis.LastModified = DateTime.UtcNow;

        CopyToEntity(saveThis, entity);

        await dataAccess.SavePersonAsync(entity);
    }

    // Hand-copying fields between the two shapes. The adapter demo moves this
    // into PersonToEntityAdapter, where it can be tested on its own.
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
        var dataAccess = new PersonDataAccess();

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
