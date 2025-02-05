using MailKit.Security;
using MimeKit;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MailKit.Net.Smtp;

namespace GraduationProjectBackendAPI.Controllers.User
{
    public class EmailQueueService
    {
        private readonly ConcurrentQueue<(string Email, string Code)> _emailQueue = new();
        private readonly IConfiguration _config;

        public EmailQueueService(IConfiguration config)
        {
            _config = config;
        }

        public void QueueEmail(string email, string code)
        {
            _emailQueue.Enqueue((email, code));
        }

        public async Task ProcessQueueAsync()
        {
            while (_emailQueue.TryDequeue(out var item))
            {
                await SendVerificationEmailAsync(item.Email, item.Code);
            }
        }

        private async Task SendVerificationEmailAsync(string emailAddress, string verificationCode)
        {
            try
            {
                var smtpServer = _config["EmailSettings:SmtpServer"];
                var port = int.Parse(_config["EmailSettings:Port"]);
                var email = _config["EmailSettings:Email"];
                var password = _config["EmailSettings:Password"];

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Graduation Project", email));
                message.To.Add(new MailboxAddress("User", emailAddress));
                message.Subject = "Email Verification Code";
                message.Body = new TextPart("plain") { Text = $"Your verification code is: {verificationCode}" };

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(email, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }

        }
    }
}
