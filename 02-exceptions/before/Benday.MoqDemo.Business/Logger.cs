namespace Benday.MoqDemo.Business;

public class Logger
{
    public void WriteToLog(Exception ex)
    {
        Console.Error.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name}: {ex.Message}");
    }
}
