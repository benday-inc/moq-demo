using Benday.Common.Testing;
using Benday.MoqDemo.Business;
using Moq;

namespace Benday.MoqDemo.UnitTests;

// The same test as PersonRegistrationServiceMoqOnlyFixture, with MockUtility.
// CreateInstance<T>() reads the constructor, creates a Moq mock for all six
// dependencies, and builds the service. The test names only the mock it cares
// about. Add a seventh dependency and this file doesn't change.
public class PersonRegistrationServiceFixture : TestClassBase
{
    public PersonRegistrationServiceFixture(ITestOutputHelper output) : base(output)
    {
    }

    private MockCreationResult<PersonRegistrationService>? _systemUnderTest;

    private MockCreationResult<PersonRegistrationService> SystemUnderTest =>
        _systemUnderTest ??= MockUtility.CreateInstance<PersonRegistrationService>();

    // MockUtility move: GetRequiredMock<T>() hands back the real Moq mock, verified with plain Moq.
    [Fact]
    public async Task RegisterAsync_WhenPersonIsNew_SendsWelcomeEmail()
    {
        // arrange
        var person = new Person { EmailAddress = TestConstants.EmailAddress };

        // act
        await SystemUnderTest.Instance.RegisterAsync(person);

        // assert
        SystemUnderTest.GetRequiredMock<IEmailSender>()
            .Verify(x => x.SendWelcomeAsync(TestConstants.EmailAddress), Times.Once);
    }
}
