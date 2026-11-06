namespace Benday.MoqDemo.Business;

// The real logger: writes to the console's error stream.
public class Logger
{
    public void WriteToLog(Exception ex)
    {
        Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
    }
}
