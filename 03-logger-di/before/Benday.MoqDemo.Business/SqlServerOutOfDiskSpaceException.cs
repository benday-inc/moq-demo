namespace Benday.MoqDemo.Business;

// A failure you can't produce on demand: SQL Server running out of disk.
// Nothing in this app ever throws it. Testing the code that handles it is the
// problem stage 02 solves with a mock.
public class SqlServerOutOfDiskSpaceException : Exception
{
    public SqlServerOutOfDiskSpaceException(string message) : base(message)
    {
    }
}
