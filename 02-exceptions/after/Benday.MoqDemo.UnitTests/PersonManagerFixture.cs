using Benday.Common.Testing;
using Benday.MoqDemo.Business;
using Benday.MoqDemo.DataAccess;
using Moq;
using Moq.Protected;

namespace Benday.MoqDemo.UnitTests;

public class PersonManagerFixture : TestClassBase
{
    public PersonManagerFixture(ITestOutputHelper output) : base(output)
    {
    }

    private Mock<PersonDataAccess>? _dataAccess;
    private Mock<PersonManager>? _partialMock;

    private Mock<PersonDataAccess> DataAccess => _dataAccess ??= new Mock<PersonDataAccess>();

    private Mock<PersonManager> PartialMock
    {
        get
        {
            if (_partialMock == null)
            {
                _partialMock = new Mock<PersonManager> { CallBase = true };

                _partialMock.Protected()
                    .Setup<PersonDataAccess>("CreateDataAccess")
                    .Returns(DataAccess.Object);
            }

            return _partialMock;
        }
    }

    private PersonManager SystemUnderTest => PartialMock.Object;

    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SetsCreateDateAndLastModifiedToNow()
    {
        var expected = new DateTime(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc);
        PartialMock.Protected().Setup<DateTime>("GetUtcNow").Returns(expected);
        var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

        await SystemUnderTest.SaveAsync(person);

        person.CreateDate.ShouldEqual(expected, "CreateDate should come from the clock seam");
        person.LastModified.ShouldEqual(expected, "LastModified should come from the clock seam");
    }

    [Fact]
    public async Task SaveAsync_WhenPersonIsNew_SavesThroughDataAccess()
    {
        var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

        await SystemUnderTest.SaveAsync(person);

        DataAccess.Verify(x => x.SavePersonAsync(It.IsAny<PersonEntity>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_FixesDiskSpaceProblem()
    {
        DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
            .ThrowsAsync(new SqlServerOutOfDiskSpaceException("No more room!"));

        await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        PartialMock.Protected().Verify("FixDiskSpaceProblem", Times.Once());
    }

    [Fact]
    public async Task SaveAsync_WhenDatabaseIsOutOfDiskSpace_RethrowsTheException()
    {
        var expected = new SqlServerOutOfDiskSpaceException("No more room!");
        DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
            .ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<SqlServerOutOfDiskSpaceException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        actual.ShouldBeSameAs(expected, "SaveAsync should rethrow the original exception");
    }

    [Fact]
    public async Task SaveAsync_WhenSaveFailsForAnotherReason_DoesNotFixDiskSpaceProblem()
    {
        DataAccess.Setup(x => x.SavePersonAsync(It.IsAny<PersonEntity>()))
            .ThrowsAsync(new InvalidOperationException("Something else broke."));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemUnderTest.SaveAsync(new Person()));

        PartialMock.Protected().Verify("FixDiskSpaceProblem", Times.Never());
    }
}
