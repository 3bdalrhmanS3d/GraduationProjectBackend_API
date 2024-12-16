using GraduationProjectBackendAPI.Models;
using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using MimeKit;
using Microsoft.AspNetCore.Authorization;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using GraduationProjectBackendAPI.DTOs;

namespace GraduationProjectBackendAPI.Controllers.User
{

    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public AccountController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("Signup")]
        public async Task<IActionResult> Signup([FromBody] UserInput userInput)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return CreateResponse("Validation failed", "error", errors);
            }

            var existingUserByEmail = await _context.UsersT.SingleOrDefaultAsync(x => x.EmailAddress == userInput.EmailAddress);
            if (existingUserByEmail != null)
            {
                if (existingUserByEmail.AccountVerification != null && existingUserByEmail.AccountVerification.CheckedOK)
                {
                    return CreateResponse("User already exists and is verified.", "error");
                }
                else
                {
                    if (existingUserByEmail.AccountVerification.Date.AddMinutes(30) < DateTime.UtcNow)
                    {
                        var newVerificationCode = GenerateVerificationCode();
                        existingUserByEmail.AccountVerification.Code = newVerificationCode;
                        existingUserByEmail.AccountVerification.Date = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        SendVerificationEmail(existingUserByEmail.EmailAddress, newVerificationCode);
                    }
                    return CreateResponse("User already exists. Please verify your email.", "error");
                }
            }

            Users newUser = new Users
            {
                FirstName = userInput.FirstName,
                LastName = userInput.LastName,
                EmailAddress = userInput.EmailAddress,
                PasswordHash = HashPassword(userInput.PasswordHash),
                CreatedAt = DateTime.UtcNow,
            };

            _context.UsersT.Add(newUser);
            await _context.SaveChangesAsync();

            var verificationCode = GenerateVerificationCode();

            AccountVerification accountVerification = new AccountVerification
            {
                UserId = newUser.UserId,
                Code = verificationCode,
                CheckedOK = false,
                Date = DateTime.UtcNow
            };
            _context.AccountVerificationT.Add(accountVerification);
            await _context.SaveChangesAsync();

            SendVerificationEmail(userInput.EmailAddress, verificationCode);

            Response.Cookies.Append("EmailForVerification", userInput.EmailAddress, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });

            return CreateResponse("Registration successful. Please check your email for the verification code.", "success");
        }

        // Verify Account endpoint
        [HttpPost("Verify-account")]
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccountInput input)
        {
            if (!Request.Cookies.TryGetValue("EmailForVerification", out string emailAddressFromCookies))
            {
                return CreateResponse("Verification email not found. Please register again.", "error");
            }

            var user = await _context.UsersT.Include(u => u.AccountVerification)
                                            .SingleOrDefaultAsync(u => u.EmailAddress == emailAddressFromCookies);

            if (user == null || user.AccountVerification == null)
            {
                return CreateResponse("User not found or verification details missing.", "error");
            }

            if (user.AccountVerification.Code != input.VerificationCode)
            {
                return CreateResponse("Invalid verification code. Please try again.", "error");
            }

            if (user.AccountVerification.Date.AddMinutes(30) < DateTime.UtcNow)
            {
                return CreateResponse("Verification code expired. Please request a new one.", "error");
            }

            user.AccountVerification.CheckedOK = true;
            await _context.SaveChangesAsync();

            Response.Cookies.Delete("EmailForVerification");

            return CreateResponse("Account verification successful. You can now sign in.", "success");
        }
        public class VerifyAccountInput
        {
            [Required]
            public string VerificationCode { get; set; }
        }

        // Sign in endpoint
        [HttpPost("Signin")]
        public async Task<IActionResult> Signin([FromBody] UserSignInInput userSignInInput)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.UsersT.Include(u => u.AccountVerification).SingleOrDefaultAsync(x => x.EmailAddress == userSignInInput.Email);
                if (existingUser == null || !VerifyPassword(userSignInInput.Password, existingUser.PasswordHash)
)
                {
                    return BadRequest("Invalid login credentials. Please try again.");
                }

                if (existingUser.AccountVerification != null && !existingUser.AccountVerification.CheckedOK)
                {
                    var newVerificationCode = GenerateVerificationCode();

                    existingUser.AccountVerification.Code = newVerificationCode;
                    existingUser.AccountVerification.Date = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    SendVerificationEmail(existingUser.EmailAddress, newVerificationCode);
                    return BadRequest("Your account is not verified. A new verification code has been sent to your email.");
                }

                UserVisitHistory newSignIn = new UserVisitHistory
                {
                    UserId = existingUser.UserId,
                    LastVisit = DateTime.UtcNow,
                };

                _context.UserVisitHistoryT.Add(newSignIn);
                await _context.SaveChangesAsync();

                JwtSecurityToken mytoken = GenerateAccessToken(
                    existingUser.UserId.ToString(),
                    existingUser.EmailAddress,
                    $"{existingUser.FirstName} {existingUser.LastName}"
                );

                var userResponse = new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(mytoken),
                    expired = mytoken.ValidTo,
                };

                return Ok(userResponse);

            }
            else
            {
                return Unauthorized(new { message = "Invalid login credentials." });
            }
        }

        // Generating token based on user information
        private JwtSecurityToken GenerateAccessToken(string userId, string email, string fullName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, fullName),
                
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SecretKey"]));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JWT:ValidIss"],
                audience: _config["JWT:ValidAud"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6), 
                signingCredentials: signingCredentials
            );

            return token;
        }

        // Forget Password endpoint
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] UserForgetPassInput userFPInput)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.UsersT.SingleOrDefaultAsync(u => u.EmailAddress == userFPInput.Email);
                if (user == null)
                {
                    return BadRequest("User does not exist with the provided email address.");
                }

                var verificationCode = GenerateVerificationCode();

                SendVerificationEmail(user.EmailAddress, verificationCode);
                return Ok("A verification code has been sent to your email address.");
            }

            return BadRequest(ModelState);
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            try
            {
                HttpContext.Session.Clear();

                foreach (var cookie in Request.Cookies.Keys)
                {
                    Response.Cookies.Delete(cookie);
                }

                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                if (!string.IsNullOrEmpty(token))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    if (jwtToken != null && jwtToken.ValidTo > DateTime.UtcNow)
                    {
                        var blacklistedToken = new BlacklistToken
                        {
                            Token = token,
                            ExpiryDate = jwtToken.ValidTo
                        };

                        _context.BlacklistTokensT.Add(blacklistedToken);
                        _context.SaveChanges();
                    }
                }

                return Ok(new
                {
                    message = "Logout successful. Session and cookies cleared.",
                    status = "success"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Logout: " + ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred during logout.",
                    status = "error"
                });
            }
        }

        [HttpPost("resend-verification-email")]
        public async Task<IActionResult> ResendVerificationEmail()
        {
            if (!Request.Cookies.TryGetValue("EmailForVerification", out string emailAddressFromCookies))
            {
                return CreateResponse("Verification email not found. Please register again.", "error");
            }

            var user = await _context.UsersT.Include(u => u.AccountVerification)
                                            .SingleOrDefaultAsync(u => u.EmailAddress == emailAddressFromCookies);

            if (user == null || user.AccountVerification == null)
            {
                return CreateResponse("User not found or verification details missing.", "error");
            }

            if (user.AccountVerification.CheckedOK)
            {
                return CreateResponse("Account is already verified.", "error");
            }

            var newVerificationCode = GenerateVerificationCode();
            user.AccountVerification.Code = newVerificationCode;
            user.AccountVerification.Date = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            SendVerificationEmail(emailAddressFromCookies, newVerificationCode);

            return CreateResponse("A new verification email has been sent.", "success");
        }

        private void SendVerificationEmail(string emailAddress, string verificationCode)
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

                message.Body = new TextPart("plain")
                {
                    Text = $"Your verification code is: {verificationCode}"
                };

                using (var client = new SmtpClient())
                {
                    try
                    {
                        client.Connect(smtpServer, port, SecureSocketOptions.StartTls);
                    }
                    catch
                    {
                        client.Connect(smtpServer, 465, SecureSocketOptions.SslOnConnect);
                    }

                    client.Authenticate(email, password);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in SendVerificationEmail: {0}", ex.ToString());
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        private string GenerateVerificationCode()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        private IActionResult CreateResponse(string message, string status, object data = null)
        {
            return Ok(new
            {
                message,
                status,
                data
            });
        }
    }

}
