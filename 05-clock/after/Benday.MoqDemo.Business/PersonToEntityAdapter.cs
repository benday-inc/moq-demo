using Benday.EfCore.Adapters;
using Benday.MoqDemo.DataAccess;

namespace Benday.MoqDemo.Business;

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
