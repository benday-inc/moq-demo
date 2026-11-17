namespace Benday.MoqDemo.UnitTests;

// Shared test values. Tests use these instead of made-up people, so anything
// that shows up in a test or a failure message is obviously test data.
public static class TestConstants
{
    public const string FirstName = "TestFirstName";
    public const string LastName = "TestLastName";
    public const string EmailAddress = "test@example.com";
    public const string Notes = "TestNotes";

    // A CreateDate that a stored record already carries. Any fixed UTC value works.
    public static readonly DateTime CreateDate = new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);
}
