namespace Benday.MoqDemo.Business;

// Answers "is this email address already registered?". One of the six
// dependencies that make PersonRegistrationService wordy to test. A Loose mock
// returns false, which here means "not a duplicate", so tests that don't care
// about duplicates get a new person by default.
public interface IDuplicateChecker
{
    Task<bool> IsDuplicateAsync(string emailAddress);
}
