using Benday.Common.Testing;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.DataAccess;
using Moq;
using Moq.Protected;

namespace Benday.MoqDemo.UnitTests;

// Tests for PersonManager. PersonManager creates its own data access, so these
// tests use a partial mock: a Mock<PersonManager> that runs the real code
// (CallBase = true) except for the protected virtual seams the tests override.
public class PersonManagerFixture(ITestOutputHelper output) : TestClassBase(output)
{
    // A mock of a concrete class. It works because PersonDataAccess's methods are virtual.
    private Mock<PersonDataAccess> DataAccess => field ??= new Mock<PersonDataAccess>();

    private Mock<PersonManager> PartialMock
    {
        get
        {
            if (field == null)
            {
                // CallBase = true: run PersonManager's real code unless a member is overridden.
                field = new Mock<PersonManager> { CallBase = true };

                // Override the protected CreateDataAccess() seam by name (it's protected, so a
                // lambda can't reach it). Now every PersonDataAccess the real code asks for is the mock.
                field.Protected()
                    .Setup<PersonDataAccess>("CreateDataAccess")
                    .Returns(DataAccess.Object);
            }

            return field;
        }
    }

    // .Object is the partial mock's PersonManager instance: real code plus overrides.
    private PersonManager SystemUnderTest => PartialMock.Object;

    // Override the GetUtcNow() seam with a fixed date, then check both timestamps used it.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow()
    {
        // arrange
        var expected = new DateTime(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc);
        PartialMock.Protected().Setup<DateTime>("GetUtcNow").Returns(expected);
        var person = new Person { FirstName = TestConstants.FirstName, LastName = TestConstants.LastName };

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        person.CreateDate.ShouldEqual(expected, "CreateDate should come from the clock seam");
        person.LastModified.ShouldEqual(expected, "LastModified should come from the clock seam");
    }

    // Moq move: Verify(..., Times.Once) checks the real code called the mocked data access exactly once.
    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SavesThroughDataAccess()
    {
        // arrange
        var person = new Person { FirstName = TestConstants.FirstName, LastName = TestConstants.LastName };

        // act
        await SystemUnderTest.SaveAsync(person);

        // assert
        DataAccess.Verify(x => x.SavePersonAsync(It.IsAny<PersonEntity>()), Times.Once);
    }
}
