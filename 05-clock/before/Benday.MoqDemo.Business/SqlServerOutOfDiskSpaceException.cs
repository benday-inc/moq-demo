namespace Benday.MoqDemo.Business;

// A failure you can't produce on demand: SQL Server running out of disk.
// Nothing in this app ever throws it, so the only way to test the code that
// handles it is a mock that throws it.
public class SqlServerOutOfDiskSpaceException : Exception
{
    public SqlServerOutOfDiskSpaceException(string message) : base(message)
    {
    }
}
