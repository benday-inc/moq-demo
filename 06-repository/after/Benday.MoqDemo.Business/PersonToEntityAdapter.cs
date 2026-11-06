using Benday.EfCore.Adapters;
using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.Business;

// Converts between Person (the domain object) and PersonEntity (the database
// shape). It's pure logic with no dependencies, so its tests use real objects
// and no mocks. AdapterBase (from Benday.EfCore) calls the two PerformAdapt
// overrides below.
public class PersonToEntityAdapter : AdapterBase<Person, PersonEntity>
{
    // The database owns PersonEntity.Id, so it is never copied onto the entity.
    protected override void PerformAdapt(Person fromValue, PersonEntity toValue)
    {
        toValue.FirstName = fromValue.FirstName;
        toValue.LastName = fromValue.LastName;
        toValue.EmailAddress = fromValue.EmailAddress;
        toValue.Notes = fromValue.Notes;
        toValue.CreateDate = fromValue.CreateDate;
        toValue.LastModified = fromValue.LastModified;
    }

    protected override void PerformAdapt(PersonEntity fromValue, Person toValue)
    {
        toValue.Id = fromValue.Id;
        toValue.FirstName = fromValue.FirstName;
        toValue.LastName = fromValue.LastName;
        toValue.EmailAddress = fromValue.EmailAddress;
        toValue.Notes = fromValue.Notes;
        toValue.CreateDate = fromValue.CreateDate;
        toValue.LastModified = fromValue.LastModified;
    }
}
