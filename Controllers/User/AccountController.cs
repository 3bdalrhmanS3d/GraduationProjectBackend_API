using GraduationProjectBackendAPI.Models;
using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace GraduationProjectBackendAPI.Controllers.User
{

    [Route("api/Account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly EmailQueueService _emailQueueService;

        public AccountController(AppDbContext context, IConfiguration config, EmailQueueService emailQueueService)
        {
            _context = context;
            _config = config;
            _emailQueueService = emailQueueService;
        }

        [HttpPost("Signup")]
        public async Task<IActionResult> Signup([FromBody] UserInput userInput)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return CreateResponse("Validation failed", "error", errors);
            }

            var existingUserByEmail = await _context.UsersT
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.EmailAddress == userInput.EmailAddress);

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

                        _emailQueueService.QueueResendEmail(userInput.EmailAddress, existingUserByEmail.FullName, newVerificationCode);


                    }
                    return CreateResponse("User already exists. Please verify your email.", "error");
                }
            }

            Users newUser = new Users
            {
                FullName = $"{userInput.FirstName} {userInput.LastName}",
                EmailAddress = userInput.EmailAddress,
                PasswordHash = HashPassword(userInput.PasswordHash),
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.RegularUser
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

            _emailQueueService.QueueEmail(userInput.EmailAddress, newUser.FullName, verificationCode);


            Response.Cookies.Append("EmailForVerification", userInput.EmailAddress, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMinutes(100)
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

        // Signin endpoint
        private static Dictionary<string, (int Attempts, DateTime LockoutEnd)> _failedLoginAttempts = new();

        [HttpPost("Signin")]
        public async Task<IActionResult> Signin([FromBody] UserSignInInput userSignInInput)
        {
            if (!ModelState.IsValid)
            {
                return Unauthorized(new { message = "Invalid login credentials." });
            }

            string email = userSignInInput.Email;

            // Lock verification 1️⃣ due to repeated failure attempts

            if (_failedLoginAttempts.ContainsKey(email) && _failedLoginAttempts[email].LockoutEnd > DateTime.UtcNow)
            {
                return BadRequest($"Too many failed attempts. Try again after {_failedLoginAttempts[email].LockoutEnd - DateTime.UtcNow:mm\\:ss} minutes.");
            }

            // 2️ User search and password verification
            var existingUser = await _context.UsersT.Include(u => u.AccountVerification)
                                   .SingleOrDefaultAsync(x => x.EmailAddress == email);

            if (existingUser == null || !VerifyPassword(userSignInInput.Password, existingUser.PasswordHash))
            {
                //Record attempt failure
                if (!_failedLoginAttempts.ContainsKey(email))
                {
                    _failedLoginAttempts[email] = (1, DateTime.UtcNow);
                }
                else
                {
                    _failedLoginAttempts[email] = (_failedLoginAttempts[email].Attempts + 1, DateTime.UtcNow);
                }

                //If failed attempts exceed 5, the user is blocked for 15 minutes
                if (_failedLoginAttempts[email].Attempts >= 5)
                {
                    _failedLoginAttempts[email] = (5, DateTime.UtcNow.AddMinutes(15));
                    return BadRequest("Too many failed login attempts. You are locked out for 15 minutes.");
                }

                return BadRequest("Invalid login credentials.");
            }

            // 3️ Checking account activation status

            if (existingUser.AccountVerification != null && !existingUser.AccountVerification.CheckedOK)
            {
                var newVerificationCode = GenerateVerificationCode();

                existingUser.AccountVerification.Code = newVerificationCode;
                existingUser.AccountVerification.Date = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _emailQueueService.QueueResendEmail(existingUser.EmailAddress, existingUser.FullName , newVerificationCode);

                return BadRequest("Your account is not verified. A new verification code has been sent to your email.");
            }

            //  4️ Reset attempts to fail when login is successful
            if (_failedLoginAttempts.ContainsKey(email))
            {
                _failedLoginAttempts.Remove(email);
            }

            //  5️ User Visit Registration
            UserVisitHistory newSignIn = new UserVisitHistory
            {
                UserId = existingUser.UserId,
                LastVisit = DateTime.UtcNow,
            };

            _context.UserVisitHistoryT.Add(newSignIn);
            await _context.SaveChangesAsync();

            // 6️ JWT token generation and sending to user

            JwtSecurityToken mytoken = GenerateAccessToken(
                existingUser.UserId.ToString(),
                existingUser.EmailAddress,
                existingUser.FullName,
                existingUser.Role
            );

            var userResponse = new
            {
                token = new JwtSecurityTokenHandler().WriteToken(mytoken),
                expired = mytoken.ValidTo,
                role = existingUser.Role.ToString() // User role
            };

            return Ok(userResponse);
        }


        // Generating token based on user information
        private JwtSecurityToken GenerateAccessToken(string userId, string email, string fullName, UserRole role )
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role.ToString() )

            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SecretKey"]));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JWT:ValidIss"],
                audience: _config["JWT:ValidAud"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // 1 hour expiration 
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

                _emailQueueService.QueueEmail(user.EmailAddress, user.FullName, verificationCode);
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

        // Verify a password against the stored hash
        private bool VerifyPassword(string password, string storedHash)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHashBytes = Convert.FromBase64String(parts[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            return hash.SequenceEqual(storedHashBytes);
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

        // Generate a secure salted hash for passwords
        private string HashPassword(string password)
        {
            using var rng = new RNGCryptoServiceProvider();
            byte[] salt = new byte[16];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }


    }

}
