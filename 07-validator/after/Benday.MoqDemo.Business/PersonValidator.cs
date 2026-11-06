using Benday.EfCore.ServiceLayers;

namespace Benday.MoqDemo.Business;

public class PersonValidator : IValidatorStrategy<Person>
{
    public bool IsValid(Person validateThis)
    {
        return string.IsNullOrWhiteSpace(validateThis.FirstName) == false
            && string.IsNullOrWhiteSpace(validateThis.LastName) == false
            && validateThis.EmailAddress.Contains('@');
    }
}
