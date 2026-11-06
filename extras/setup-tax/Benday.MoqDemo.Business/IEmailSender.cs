namespace Benday.MoqDemo.Business;

// Sends email. The one dependency RegisterAsync_WhenPersonIsNew_SendsWelcomeEmail
// actually cares about.
public interface IEmailSender
{
    Task SendWelcomeAsync(string emailAddress);
}
