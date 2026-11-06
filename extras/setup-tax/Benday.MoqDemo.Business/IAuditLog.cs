namespace Benday.MoqDemo.Business;

// Records what happened, for the people who ask "who registered this?" later.
// Another dependency that most tests don't care about.
public interface IAuditLog
{
    Task RecordAsync(string message);
}
