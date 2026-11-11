using Benday.Common.Testing;
using Benday.MoqDemo.Business;

namespace Benday.MoqDemo.UnitTests;

// Tests for the real validation rules, with real Person objects and no mocks.
// PersonManagerFixture mocks the validator; this file proves the rules.
public class PersonValidatorFixture : TestClassBase
{
    public PersonValidatorFixture(ITestOutputHelper output) : base(output)
    {
    }

    private PersonValidator? _systemUnderTest;

    private PersonValidator SystemUnderTest => _systemUnderTest ??= new PersonValidator();

    // Start from a valid person, then break one thing per test.
    private static Person CreateValidPerson() => new()
    {
        FirstName = TestConstants.FirstName,
        LastName = TestConstants.LastName,
        EmailAddress = TestConstants.EmailAddress
    };

    [Fact]
    public void IsValid_WhenNameAndEmailArePresent_ReturnsTrue()
    {
        var person = CreateValidPerson();

        SystemUnderTest.IsValid(person).ShouldBeTrue("a person with a name and an email is valid");
    }

    [Fact]
    public void IsValid_WhenFirstNameIsMissing_ReturnsFalse()
    {
        var person = CreateValidPerson();
        person.FirstName = " ";

        SystemUnderTest.IsValid(person).ShouldBeFalse("first name is required");
    }

    [Fact]
    public void IsValid_WhenLastNameIsMissing_ReturnsFalse()
    {
        var person = CreateValidPerson();
        person.LastName = string.Empty;

        SystemUnderTest.IsValid(person).ShouldBeFalse("last name is required");
    }

    [Fact]
    public void IsValid_WhenEmailAddressHasNoAtSign_ReturnsFalse()
    {
        var person = CreateValidPerson();
        person.EmailAddress = "test.example.com";

        SystemUnderTest.IsValid(person).ShouldBeFalse("an email address needs an @");
    }
}
