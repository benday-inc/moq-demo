using Benday.Common.Interfaces;
using Benday.Common.Testing;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.Common;
using Moq;

namespace Benday.MoqDemo.UnitTests;

// The setup tax, paid in full. PersonRegistrationService has six constructor
// dependencies, so plain Moq needs six Mock<T> fields and a constructor call
// that passes every .Object, even though the test below cares about one.
// PersonRegistrationServiceFixture has the same test with MockUtility.
public class PersonRegistrationServiceMoqOnlyFixture : TestClassBase
{
    public PersonRegistrationServiceMoqOnlyFixture(ITestOutputHelper output) : base(output)
    {
    }

    // Fields are safe here because xUnit creates a new fixture instance for every test.
    private readonly Mock<IAsyncReadableRepository<PersonEntity, int>> _repository = new();
    private readonly Mock<IDuplicateChecker> _duplicateChecker = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IAuditLog> _auditLog = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly Mock<TimeProvider> _clock = new();

    // Add a seventh dependency and this line has to change in every fixture that builds the service.
    private PersonRegistrationService CreateSystemUnderTest() =>
        new PersonRegistrationService(
            _repository.Object, _duplicateChecker.Object, _emailSender.Object,
            _auditLog.Object, _logger.Object, _clock.Object);

    // Moq move: Verify with Times.Once, on the one mock out of six this test cares about.
    [Fact]
    public async Task RegisterAsync_WhenPersonIsNew_SendsWelcomeEmail()
    {
        var person = new Person { EmailAddress = "ada@example.com" };

        await CreateSystemUnderTest().RegisterAsync(person);

        _emailSender.Verify(x => x.SendWelcomeAsync("ada@example.com"), Times.Once);
    }
}
