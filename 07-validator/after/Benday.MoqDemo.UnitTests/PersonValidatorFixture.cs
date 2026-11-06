using Benday.Common.Testing;
using Benday.MoqDemo.Business;

namespace Benday.MoqDemo.UnitTests;

public class PersonValidatorFixture : TestClassBase
{
    public PersonValidatorFixture(ITestOutputHelper output) : base(output)
    {
    }

    private PersonValidator? _systemUnderTest;

    private PersonValidator SystemUnderTest => _systemUnderTest ??= new PersonValidator();

    private static Person CreateValidPerson() => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        EmailAddress = "ada@example.com"
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
        person.EmailAddress = "ada.example.com";

        SystemUnderTest.IsValid(person).ShouldBeFalse("an email address needs an @");
    }
}
