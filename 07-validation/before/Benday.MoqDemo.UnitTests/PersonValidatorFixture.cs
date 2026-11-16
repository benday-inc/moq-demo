using Benday.Common.Testing;
using Benday.MoqDemo.Business;

namespace Benday.MoqDemo.UnitTests;

// Tests for the real validation rules, with real Person objects and no mocks.
// PersonManagerFixture mocks the validator; this file proves the rules.
public class PersonValidatorFixture(ITestOutputHelper output) : TestClassBase(output)
{
    private PersonValidator SystemUnderTest => field ??= new PersonValidator();

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
        // arrange
        var person = CreateValidPerson();

        // act
        var actual = SystemUnderTest.IsValid(person);

        // assert
        actual.ShouldBeTrue("a person with a name and an email is valid");
    }

    [Fact]
    public void IsValid_WhenFirstNameIsMissing_ReturnsFalse()
    {
        // arrange
        var person = CreateValidPerson();
        person.FirstName = " ";

        // act
        var actual = SystemUnderTest.IsValid(person);

        // assert
        actual.ShouldBeFalse("first name is required");
    }

    [Fact]
    public void IsValid_WhenLastNameIsMissing_ReturnsFalse()
    {
        // arrange
        var person = CreateValidPerson();
        person.LastName = string.Empty;

        // act
        var actual = SystemUnderTest.IsValid(person);

        // assert
        actual.ShouldBeFalse("last name is required");
    }

    [Fact]
    public void IsValid_WhenEmailAddressHasNoAtSign_ReturnsFalse()
    {
        // arrange
        var person = CreateValidPerson();
        person.EmailAddress = "test.example.com";

        // act
        var actual = SystemUnderTest.IsValid(person);

        // assert
        actual.ShouldBeFalse("an email address needs an @");
    }
}
