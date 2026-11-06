using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.Business;

// The real logger: writes to the console's error stream. It implements ILogger,
// so tests can hand PersonManager a Mock<ILogger> instead.
public class Logger : ILogger
{
    public void WriteToLog(Exception ex)
    {
        Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
    }
}
