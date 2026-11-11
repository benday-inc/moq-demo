using Benday.EfCore.DomainModels;

namespace Benday.MoqDemo.Business;

// The domain object the app works with. It knows nothing about the database.
// DomainModelBase (from Benday.EfCore) supplies the Id property.
public class Person : DomainModelBase
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime LastModified { get; set; }
}
