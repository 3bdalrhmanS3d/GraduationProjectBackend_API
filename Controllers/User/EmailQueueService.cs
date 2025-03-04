using MailKit.Security;
using MimeKit;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MailKit.Net.Smtp;

namespace GraduationProjectBackendAPI.Controllers.User
{
    public class EmailQueueService
    {
        private readonly ConcurrentQueue<(string Email, string FullName, string Code, bool IsResend)> _emailQueue = new();
        private readonly IConfiguration _config;

        public EmailQueueService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Queue a new email for sending.
        /// </summary>
        public void QueueEmail(string email, string fullName, string code)
        {
            _emailQueue.Enqueue((email, fullName, code, false)); // Normal email
        }

        /// <summary>
        /// Queue a resend email request.
        /// </summary>
        public void QueueResendEmail(string email, string fullName, string code)
        {
            _emailQueue.Enqueue((email, fullName, code, true)); // Resend email
        }

        /// <summary>
        /// Processes queued emails asynchronously.
        /// </summary>
        public async Task ProcessQueueAsync()
        {
            while (_emailQueue.TryDequeue(out var item))
            {
                await SendVerificationEmailAsync(item.Email, item.FullName , item.Code, item.IsResend);
            }
        }

        /// <summary>
        /// Sends a verification email.
        /// </summary>
        private async Task SendVerificationEmailAsync(string emailAddress, string fullName , string verificationCode, bool isResend)
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

                message.Subject = isResend ? "🔄 Resend: Email Verification Code" : "✅ Your Email Verification Code";

                string emailBody = $@"
                                    <html>
                                        <head>
                                            <style>
                                                body {{
                                                    font-family: Arial, sans-serif;
                                                    background-color: #f4f4f4;
                                                    text-align: center;
                                                }}
                                                .container {{
                                                    max-width: 500px;
                                                    margin: 20px auto;
                                                    padding: 20px;
                                                    background-color: #ffffff;
                                                    border-radius: 10px;
                                                    box-shadow: 0px 4px 6px rgba(0, 0, 0, 0.1);
                                                }}
                                                h2 {{
                                                    color: #2d89ef;
                                                }}
                                                .code {{
                                                    font-size: 24px;
                                                    font-weight: bold;
                                                    color: #d9534f;
                                                    background-color: #f8d7da;
                                                    padding: 10px 20px;
                                                    display: inline-block;
                                                    border-radius: 5px;
                                                }}
                                                .footer {{
                                                    margin-top: 20px;
                                                    font-size: 12px;
                                                    color: #777;
                                                }}
                                            </style>
                                        </head>
                                        <body>
                                            <div class='container'>
                                                <h2>{(isResend ? "🔄 Resend: Your New Verification Code" : "✅ Your Email Verification Code")}</h2>
                                                <p>Hello, {fullName}</p>
                                                <p>{(isResend ? "You have requested a new verification code. Please use the code below:" : "Please use the following verification code to complete your registration:")}</p>
                                                <div class='code'>{verificationCode}</div>
                                                <p>If you did not request this email, please ignore it.</p>
                                                <div class='footer'>
                                                    <p>Graduation Project Team</p>
                                                    <p>Contact us: <a href='mailto:support@graduationproject.com'>support@graduationproject.com</a></p>
                                                </div>
                                            </div>
                                        </body>
                                    </html>";

                message.Body = new TextPart("html") { Text = emailBody };

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
