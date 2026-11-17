using Benday.Common.Interfaces;
using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.Business;

// The system under test. The repository comes in through the constructor, so
// a test can pass a mock and never touch SQL Server. The logger and the clock
// are still created inline.
public class PersonManager
{
    private readonly IAsyncReadableRepository<PersonEntity, int> _repository;

    // Created with new, not injected: the adapter is pure logic, so tests use the
    // real one. A mocked adapter would silently copy nothing.
    private readonly PersonToEntityAdapter _adapter = new();

    // Constructor injection: PersonManager advertises what it depends on, and a
    // test can pass mocks for any of it.
    public PersonManager(IAsyncReadableRepository<PersonEntity, int> repository)
    {
        _repository = repository;
    }

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
        // Problem: the real clock. A test can't know what time this will be.
        saveThis.CreateDate = DateTime.UtcNow;
        saveThis.LastModified = DateTime.UtcNow;

        var entity = new PersonEntity();
        _adapter.Adapt(saveThis, entity);

        await _repository.SaveAsync(entity);

        saveThis.Id = entity.Id;
    }

    private async Task SaveExistingAsync(Person saveThis)
    {
        var entity = await _repository.GetByIdAsync(saveThis.Id)
            ?? throw new InvalidOperationException("Unknown person.");

        // The database owns CreateDate. The stored value wins over whatever the
        // caller's copy says, so an update can't wipe it out.
        saveThis.CreateDate = entity.CreateDate;
        saveThis.LastModified = DateTime.UtcNow;

        _adapter.Adapt(saveThis, entity);

        await _repository.SaveAsync(entity);
    }

    public async Task<Person?> LoadByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);

        if (entity == null)
        {
            return null;
        }

        var person = new Person();
        _adapter.Adapt(entity, person);

        return person;
    }
}
