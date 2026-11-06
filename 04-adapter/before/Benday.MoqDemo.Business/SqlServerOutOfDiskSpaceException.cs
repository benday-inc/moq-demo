namespace Benday.MoqDemo.Business;

public class SqlServerOutOfDiskSpaceException : Exception
{
    public SqlServerOutOfDiskSpaceException(string message) : base(message)
    {
    }
}
