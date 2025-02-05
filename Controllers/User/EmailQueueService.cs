using MailKit.Security;
using MimeKit;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MailKit.Net.Smtp;

namespace GraduationProjectBackendAPI.Controllers.User
{
    public class EmailQueueService
    {
        private readonly ConcurrentQueue<(string Email, string Code, bool IsResend)> _emailQueue = new();
        private readonly IConfiguration _config;

        public EmailQueueService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Queue a new email for sending.
        /// </summary>
        public void QueueEmail(string email, string code)
        {
            _emailQueue.Enqueue((email, code, false)); // Normal email
        }

        /// <summary>
        /// Queue a resend email request.
        /// </summary>
        public void QueueResendEmail(string email, string code)
        {
            _emailQueue.Enqueue((email, code, true)); // Resend email
        }

        /// <summary>
        /// Processes queued emails asynchronously.
        /// </summary>
        public async Task ProcessQueueAsync()
        {
            while (_emailQueue.TryDequeue(out var item))
            {
                await SendVerificationEmailAsync(item.Email, item.Code, item.IsResend);
            }
        }

        /// <summary>
        /// Sends a verification email.
        /// </summary>
        private async Task SendVerificationEmailAsync(string emailAddress, string verificationCode, bool isResend)
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

                message.Subject = isResend ? "Resend: Email Verification Code" : "Email Verification Code";
                message.Body = new TextPart("plain")
                {
                    Text = isResend
                        ? $"You requested a new verification code. Your new verification code is: {verificationCode}"
                        : $"Your verification code is: {verificationCode}"
                };

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(email, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"Email sent successfully to {emailAddress}. Resend: {isResend}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email to {emailAddress}: {ex.Message}");
            }
        }
    }
}
