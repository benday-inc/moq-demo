namespace Benday.MoqDemo.Business;

// The domain object the app works with. It knows nothing about the database.
public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime LastModified { get; set; }
}
