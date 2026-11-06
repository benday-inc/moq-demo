using Benday.Common.Interfaces;
using Benday.EfCore.ServiceLayers;
using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.Business;

public class PersonManager
{
    private readonly IAsyncReadableRepository<PersonEntity, int> _repository;
    private readonly ILogger _logger;
    private readonly IValidatorStrategy<Person> _validator;
    private readonly TimeProvider _timeProvider;
    private readonly PersonToEntityAdapter _adapter = new();

    public PersonManager(
        IAsyncReadableRepository<PersonEntity, int> repository,
        ILogger logger,
        IValidatorStrategy<Person> validator,
        TimeProvider timeProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Person CreateNew()
    {
        return new Person();
    }

    public async Task SaveAsync(Person saveThis)
    {
        if (_validator.IsValid(saveThis) == false)
        {
            throw new InvalidObjectException("Person is not valid.");
        }

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

    private void FixDiskSpaceProblem()
    {
        Console.WriteLine("Hey, somebody should really do something about that database. It's outta disk space.");
    }

    private async Task SaveNewAsync(Person saveThis)
    {
        saveThis.CreateDate = _timeProvider.GetUtcNow().UtcDateTime;
        saveThis.LastModified = _timeProvider.GetUtcNow().UtcDateTime;

        var entity = new PersonEntity();
        _adapter.Adapt(saveThis, entity);

        await _repository.SaveAsync(entity);

        saveThis.Id = entity.Id;
    }

    private async Task SaveExistingAsync(Person saveThis)
    {
        var entity = await _repository.GetByIdAsync(saveThis.Id)
            ?? throw new InvalidOperationException("Unknown person.");

        saveThis.LastModified = _timeProvider.GetUtcNow().UtcDateTime;

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
