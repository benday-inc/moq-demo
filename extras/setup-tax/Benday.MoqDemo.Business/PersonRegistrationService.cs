using Benday.Common.Interfaces;
using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.Business;

// A service with six constructor dependencies. It exists to show the "setup
// tax": with plain Moq every test fixture has to create and pass all six mocks,
// even when a test cares about one. PersonRegistrationServiceMoqOnlyFixture
// pays that tax; PersonRegistrationServiceFixture lets MockUtility pay it.
public class PersonRegistrationService
{
    private readonly IAsyncReadableRepository<PersonEntity, int> _repository;
    private readonly IDuplicateChecker _duplicateChecker;
    private readonly IEmailSender _emailSender;
    private readonly IAuditLog _auditLog;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;

    // Pure logic, so it's created with new and never mocked.
    private readonly PersonToEntityAdapter _adapter = new();

    public PersonRegistrationService(
        IAsyncReadableRepository<PersonEntity, int> repository,
        IDuplicateChecker duplicateChecker,
        IEmailSender emailSender,
        IAuditLog auditLog,
        ILogger logger,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _duplicateChecker = duplicateChecker;
        _emailSender = emailSender;
        _auditLog = auditLog;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    // Saves a new person and sends the welcome email. A duplicate email address
    // is recorded in the audit log and otherwise ignored.
    public async Task RegisterAsync(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        try
        {
            if (await _duplicateChecker.IsDuplicateAsync(person.EmailAddress))
            {
                await _auditLog.RecordAsync($"Duplicate registration: {person.EmailAddress}");
                return;
            }

            await SaveNewAsync(person);

            await _emailSender.SendWelcomeAsync(person.EmailAddress);
            await _auditLog.RecordAsync($"Registered: {person.EmailAddress}");
        }
        catch (Exception ex)
        {
            _logger.WriteToLog(ex);
            throw;
        }
    }

    private async Task SaveNewAsync(Person person)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        person.CreateDate = now;
        person.LastModified = now;

        var entity = new PersonEntity();
        _adapter.Adapt(person, entity);

        await _repository.SaveAsync(entity);

        person.Id = entity.Id;
    }
}
