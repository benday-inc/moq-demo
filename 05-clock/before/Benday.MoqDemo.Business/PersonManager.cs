using Benday.MoqDemo.Common;
using Benday.MoqDemo.DataAccess;

namespace Benday.MoqDemo.Business;

public class PersonManager
{
    private readonly ILogger _logger;
    private readonly PersonToEntityAdapter _adapter = new();

    public PersonManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        catch (SqlServerOutOfDiskSpaceException ex)
        {
            FixDiskSpaceProblem();
            _logger.WriteToLog(ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.WriteToLog(ex);
            throw;
        }
    }

    // Seam for tests; do not inline.
    protected virtual PersonDataAccess CreateDataAccess() => new PersonDataAccess();

    // Seam for tests; do not inline.
    protected virtual DateTime GetUtcNow() => DateTime.UtcNow;

    protected virtual void FixDiskSpaceProblem()
    {
        Console.WriteLine("Hey, somebody should really do something about that database. It's outta disk space.");
    }

    private async Task SaveNewAsync(Person saveThis)
    {
        var dataAccess = CreateDataAccess();

        saveThis.CreateDate = GetUtcNow();
        saveThis.LastModified = GetUtcNow();

        var entity = new PersonEntity();
        _adapter.Adapt(saveThis, entity);

        await dataAccess.SavePersonAsync(entity);

        saveThis.Id = entity.Id;
    }

    private async Task SaveExistingAsync(Person saveThis)
    {
        var dataAccess = CreateDataAccess();

        var entity = await dataAccess.LoadPersonAsync(saveThis.Id)
            ?? throw new InvalidOperationException("Unknown person.");

        saveThis.LastModified = GetUtcNow();

        _adapter.Adapt(saveThis, entity);

        await dataAccess.SavePersonAsync(entity);
    }

    public async Task<Person?> LoadByIdAsync(int id)
    {
        var dataAccess = CreateDataAccess();

        var entity = await dataAccess.LoadPersonAsync(id);

        if (entity == null)
        {
            return null;
        }

        var person = new Person();
        _adapter.Adapt(entity, person);

        return person;
    }
}
