namespace Benday.MoqDemo.Business;

public class Logger
{
    public void WriteToLog(Exception ex)
    {
        Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
    }
}
