namespace Benday.MoqDemo.Common;

// Extracted from Logger so PersonManager can depend on an interface, and tests
// can pass a Mock<ILogger>.
public interface ILogger
{
    void WriteToLog(Exception ex);
}
