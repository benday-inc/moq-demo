using Benday.EfCore.Entities;

namespace Benday.MoqDemo.DataAccess;

// The EF Core entity: the shape of the Persons table. EntityBase (from
// Benday.EfCore) supplies the Id property.
public class PersonEntity : EntityBase
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime LastModified { get; set; }
}
