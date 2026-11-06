using Benday.MoqDemo.Common;

namespace Benday.MoqDemo.Business;

public class Logger : ILogger
{
    public void WriteToLog(Exception ex)
    {
        Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
    }
}
