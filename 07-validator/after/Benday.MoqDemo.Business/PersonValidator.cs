using Benday.EfCore.ServiceLayers;

namespace Benday.MoqDemo.Business;

// The real validation rules. PersonManager receives them through
// IValidatorStrategy<Person>, so PersonManager's tests can mock the validator,
// and PersonValidatorFixture tests these rules with real Person objects.
public class PersonValidator : IValidatorStrategy<Person>
{
    public bool IsValid(Person validateThis)
    {
        return string.IsNullOrWhiteSpace(validateThis.FirstName) == false
            && string.IsNullOrWhiteSpace(validateThis.LastName) == false
            && validateThis.EmailAddress.Contains('@');
    }
}
