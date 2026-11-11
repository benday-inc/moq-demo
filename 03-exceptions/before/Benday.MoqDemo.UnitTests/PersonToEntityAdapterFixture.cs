using Benday.Common.Testing;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.UnitTests;

// Tests for PersonToEntityAdapter, with real objects and no mocks. It's pure
// shape conversion, so there's nothing to mock. FakeValueGenerator fills the
// source object with predictable values, so a field that isn't copied fails.
public class PersonToEntityAdapterFixture : TestClassBase
{
    public PersonToEntityAdapterFixture(ITestOutputHelper output) : base(output)
    {
    }

    private PersonToEntityAdapter? _systemUnderTest;

    private PersonToEntityAdapter SystemUnderTest => _systemUnderTest ??= new PersonToEntityAdapter();

    // Every field should land on the entity.
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

    // The database owns the entity's Id, so the adapter must leave it alone.
    [Fact]
    public void Adapt_FromPersonToEntity_DoesNotCopyTheId()
    {
        var fromValue = FakeValueGenerator.Create<Person>();
        var toValue = new PersonEntity { Id = 42 };

        SystemUnderTest.Adapt(fromValue, toValue);

        toValue.Id.ShouldEqual(42, "The database owns the entity's Id");
    }

    // The other direction, including the Id.
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
