using System.Net;
using System.Net.Mail;

namespace Group6WebProject.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlContent);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");

        using (var client = new SmtpClient())
        {
            client.Host = emailSettings["MailServer"];
            client.Port = int.Parse(emailSettings["MailPort"]);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(emailSettings["Sender"], emailSettings["Password"]);

            using (var emailMessage = new MailMessage())
            {
                emailMessage.To.Add(new MailAddress(to));
                emailMessage.From = new MailAddress(emailSettings["Sender"], emailSettings["SenderName"]);
                emailMessage.Subject = subject;
                emailMessage.Body = htmlContent;
                emailMessage.IsBodyHtml = true;

                await client.SendMailAsync(emailMessage);
            }
        }
    }
}