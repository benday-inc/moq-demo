using Benday.Common.Testing;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.UnitTests;

public class PersonToEntityAdapterFixture : TestClassBase
{
    public PersonToEntityAdapterFixture(ITestOutputHelper output) : base(output)
    {
    }

    private PersonToEntityAdapter? _systemUnderTest;

    private PersonToEntityAdapter SystemUnderTest => _systemUnderTest ??= new PersonToEntityAdapter();

    [Fact]
    public void Adapt_FromPersonToEntity_CopiesTheFields()
    {
        var fromValue = FakeValueGenerator.Create<Person>();
        var toValue = new PersonEntity();

        SystemUnderTest.Adapt(fromValue, toValue);

        toValue.FirstName.ShouldEqual(fromValue.FirstName, "FirstName should be copied");
        toValue.LastName.ShouldEqual(fromValue.LastName, "LastName should be copied");
        toValue.EmailAddress.ShouldEqual(fromValue.EmailAddress, "EmailAddress should be copied");
        toValue.Notes.ShouldEqual(fromValue.Notes, "Notes should be copied");
        toValue.CreateDate.ShouldEqual(fromValue.CreateDate, "CreateDate should be copied");
        toValue.LastModified.ShouldEqual(fromValue.LastModified, "LastModified should be copied");
    }

    [Fact]
    public void Adapt_FromPersonToEntity_DoesNotCopyTheId()
    {
        var fromValue = FakeValueGenerator.Create<Person>();
        var toValue = new PersonEntity { Id = 42 };

        SystemUnderTest.Adapt(fromValue, toValue);

        toValue.Id.ShouldEqual(42, "The database owns the entity's Id");
    }

    [Fact]
    public void Adapt_FromEntityToPerson_CopiesTheFieldsAndTheId()
    {
        var fromValue = FakeValueGenerator.Create<PersonEntity>();
        var toValue = new Person();

        SystemUnderTest.Adapt(fromValue, toValue);

        toValue.Id.ShouldEqual(fromValue.Id, "Id should be copied");
        toValue.FirstName.ShouldEqual(fromValue.FirstName, "FirstName should be copied");
        toValue.LastName.ShouldEqual(fromValue.LastName, "LastName should be copied");
        toValue.EmailAddress.ShouldEqual(fromValue.EmailAddress, "EmailAddress should be copied");
        toValue.Notes.ShouldEqual(fromValue.Notes, "Notes should be copied");
        toValue.CreateDate.ShouldEqual(fromValue.CreateDate, "CreateDate should be copied");
        toValue.LastModified.ShouldEqual(fromValue.LastModified, "LastModified should be copied");
    }
}
