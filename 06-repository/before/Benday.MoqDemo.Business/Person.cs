using Benday.EfCore.DomainModels;

namespace Benday.MoqDemo.Business;

public class Person : DomainModelBase
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime LastModified { get; set; }
}
