﻿using GraduationProjectBackendAPI.Models;
using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

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

        public class UserInput
        {
            [Required]
            [StringLength(50)]
            public string FirstName { get; set; }

            [Required]
            [StringLength(50)]
            public string LastName { get; set; }

            [Required]
            [EmailAddress]
            public string EmailAddress { get; set; }

            [Required]
            public string PasswordHash { get; set; }

            [Required]
            [Compare("PasswordHash", ErrorMessage = "The fields Password and PasswordConfirmation should be equals")]
            public string userConfPassword { get; set; }
        }

        public class UserSignInInput
        {
            [Required(ErrorMessage = "Email Address is required.")]
            [EmailAddress(ErrorMessage = "Invalid Email Address.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            public string Password { get; set; }
        }

        public class UserFPInput
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid Email Address.")]
            public string Email { get; set; }
        }

        // Sign up endpoint
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] UserInput userInput)
        {
            if (ModelState.IsValid)
            {
                var existingUserByEmail = await _context.UsersT.SingleOrDefaultAsync(x => x.EmailAddress == userInput.EmailAddress);
                if (existingUserByEmail != null)
                {
                    if (existingUserByEmail.AccountVerification != null && existingUserByEmail.AccountVerification.CheckedOK)
                    {
                        return BadRequest("User already exists and is verified.");
                    }
                    else
                    {
                        return BadRequest("User already exists. Please verify your email.");
                    }
                }
                else
                {
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

                    var verificationCode = new Random().Next(100000, 999999).ToString();
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
                    // Save email in cookies
                    Response.Cookies.Append("EmailForVerification", userInput.EmailAddress, new CookieOptions
                    {
                        HttpOnly = true,
                        Expires = DateTime.UtcNow.AddMinutes(30) // Set expiration as per your requirement
                    });

                    return Ok("Registration successful. Please check your email for the verification code.");
                }
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        // Verify Account endpoint
        [HttpPost("verify-account")]
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccountInput input)
        {
            // Retrieve email from cookies
            if (!Request.Cookies.TryGetValue("EmailForVerification", out string emailAddressFromCookies))
            {
                return BadRequest("Verification email not found. Please register again.");
            }

            var user = await _context.UsersT.Include(u => u.AccountVerification).SingleOrDefaultAsync(u => u.EmailAddress == emailAddressFromCookies);
            if (user != null && user.AccountVerification != null && user.AccountVerification.Code == input.VerificationCode)
            {
                user.AccountVerification.CheckedOK = true;
                await _context.SaveChangesAsync();

                Response.Cookies.Delete("EmailForVerification");

                return Ok("Account verification successful. You can now sign in.");
            }
            else
            {
                return BadRequest("Invalid verification code. Please try again.");
            }
        }


        public class VerifyAccountInput
        {
            [Required]
            public string VerificationCode { get; set; }
        }

        // Sign in endpoint
        [HttpPost("signin")]
        public async Task<IActionResult> Signin([FromBody] UserSignInInput userSignInInput)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.UsersT.Include(u => u.AccountVerification).SingleOrDefaultAsync(x => x.EmailAddress == userSignInInput.Email);
                if (existingUser == null)
                {
                    return BadRequest("User does not exist. Please sign up.");
                }

                if (existingUser.PasswordHash != HashPassword(userSignInInput.Password))
                {
                    return BadRequest("Invalid email or password. Please try again.");
                }

                if (existingUser.AccountVerification != null && !existingUser.AccountVerification.CheckedOK)
                {
                    var newVerificationCode = new Random().Next(100000, 999999).ToString();
                    existingUser.AccountVerification.Code = newVerificationCode;
                    existingUser.AccountVerification.Date = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    SendVerificationEmail(existingUser.EmailAddress, newVerificationCode);
                    return BadRequest("Your account is not verified. A new verification code has been sent to your email.");
                }

                //HttpContext.Session.SetInt32("UID", existingUser.UserId);
                //HttpContext.Session.SetString("FullName", $"{existingUser.FirstName} {existingUser.LastName}");

                List<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, existingUser.UserId.ToString()));
                claims.Add(new Claim(ClaimTypes.Email, existingUser.EmailAddress.ToString()));
                claims.Add(new Claim(ClaimTypes.Name, $"{existingUser.FirstName} {existingUser.LastName}") );

                // إنشاء مفتاح التوقيع
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SecretKey"]));
                SigningCredentials signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                UserVisitHistory newSignIn = new UserVisitHistory
                {
                    UserId = existingUser.UserId,
                    LastVisit = DateTime.UtcNow,
                };

                _context.UserVisitHistoryT.Add(newSignIn);
                await _context.SaveChangesAsync();

                JwtSecurityToken mytoken = new JwtSecurityToken(
                    issuer: _config["JWT:ValidIss"],
                    audience: _config["JWT:ValidAud"],
                    expires: DateTime.Now.AddHours(1),
                    claims: claims,
                    signingCredentials: signingCredentials
                );

                var userResponse = new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(mytoken),
                    expired = mytoken.ValidTo,
                    userId = existingUser.UserId,
                    fullName = $"{existingUser.FirstName} {existingUser.LastName}",
                    email = existingUser.EmailAddress,
                };

                return Ok(userResponse);
            }
            else
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }
        }

        // Forget Password endpoint
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] UserFPInput userFPInput)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.UsersT.SingleOrDefaultAsync(u => u.EmailAddress == userFPInput.Email);
                if (user == null)
                {
                    return BadRequest("User does not exist with the provided email address.");
                }

                var verificationCode = new Random().Next(100000, 999999).ToString();
                SendVerificationEmail(user.EmailAddress, verificationCode);
                return Ok("A verification code has been sent to your email address.");
            }

            return BadRequest(ModelState);
        }

        // POST: api/Logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            return Ok(new { message = "User logged out and session and cookies cleared successfully." });
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


        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);

                StringBuilder result = new StringBuilder();
                foreach (byte b in hash)
                {
                    result.Append(b.ToString("x2"));
                }

                return result.ToString();
            }
        }
    }
}