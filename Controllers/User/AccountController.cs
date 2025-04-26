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
using GraduationProjectBackendAPI.Controllers.DOT.User;
using GraduationProjectBackendAPI.Controllers.Services;

namespace GraduationProjectBackendAPI.Controllers.User
{

    [Route("api/Account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly EmailQueueService _emailQueueService;
        private readonly FailedLoginTracker _failedLoginTracker;

        public AccountController(AppDbContext context, IConfiguration config, EmailQueueService emailQueueService , FailedLoginTracker failedLoginTracker)
        {
            _context = context;
            _config = config;
            _emailQueueService = emailQueueService;
            _failedLoginTracker = failedLoginTracker;
        }

        #region
        /* 
        ==========================================
        Signup Endpoint - User Registration
        ==========================================

        🔹 This endpoint is responsible for user registration.
        🔹 It checks if the user already exists and handles email verification.
        🔹 If the user is new, they receive an email with a verification code.
        🔹 If the user has an unverified account, they must verify their email first.

        Key Features & Improvements:
        ------------------------------------------------
        ✔ Prevents spamming by limiting email verification code resends to once every 30 minutes.
        ✔ Optimized database queries by fetching only necessary user data.
        ✔ Ensures email uniqueness before allowing a new user to register.
        ✔ Uses secure cookies for verification tracking.
        ✔ Reduces database operations with a single `SaveChangesAsync()` call when possible.
        ✔ Provides meaningful error messages to improve user experience.

        Important Notes for Future Updates:
        ------------------------------------------------
        🔸 If the verification process needs changes, ensure `AccountVerificationT` logic is updated accordingly.
        🔸 If adding new authentication mechanisms (OAuth, SSO), update this flow to handle different account states.
        🔸 If password complexity rules change, ensure `HashPassword()` remains synchronized.

        Developer Notes:
        ------------------------------------------------
        - When modifying this logic, ensure that email throttling and security mechanisms are intact.
        - Avoid exposing detailed error messages in production for security reasons.
        - Consider logging unsuccessful registration attempts for security analysis.

        Related Components:
        - `VerifyAccount()` (Handles email verification)
        - `Signin()` (Handles login and failed attempts tracking)
        - `EmailQueueService` (Handles queued email sending)

        ==========================================
        Developed & Maintained by: [abdo]
        Last Updated: [4 march 25]
        ==========================================
        */
        #endregion

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
                .Include(u=> u.AccountVerification)
                .SingleOrDefaultAsync(x => x.EmailAddress == userInput.EmailAddress);

            if (existingUserByEmail != null)
            {
                if (existingUserByEmail.AccountVerification != null && existingUserByEmail.AccountVerification.CheckedOK)
                {
                    return CreateResponse("User already exists and is verified.", "error");
                }

                if (existingUserByEmail.AccountVerification != null)
                {
                    var lastSentTime = existingUserByEmail.AccountVerification.Date;
                    var timeSinceLastSent = DateTime.UtcNow - lastSentTime;

                    if (timeSinceLastSent.TotalMinutes < 30)
                    {
                        return CreateResponse(
                            $"A verification code was already sent. Please wait {30 - (int)timeSinceLastSent.TotalMinutes} minutes before requesting a new one.",
                            "error"
                        );
                    }

                    // Update the verification code after 30 minutes
                    var newVerificationCode = GenerateVerificationCode();
                    existingUserByEmail.AccountVerification.Code = newVerificationCode;
                    existingUserByEmail.AccountVerification.Date = DateTime.UtcNow;
                }
                else
                {
                    // Create a verification code if it does not exist
                    existingUserByEmail.AccountVerification = new AccountVerification
                    {
                        UserId = existingUserByEmail.UserId,
                        Code = GenerateVerificationCode(),
                        CheckedOK = false,
                        Date = DateTime.UtcNow
                    };
                }

                await _context.SaveChangesAsync();
                _emailQueueService.QueueResendEmail(userInput.EmailAddress, existingUserByEmail.FullName, existingUserByEmail.AccountVerification.Code);
                return CreateResponse("User already exists. Please verify your email.", "error");
            }

            Users newUser = new Users
            {
                FullName = $"{userInput.FirstName} {userInput.LastName}",
                EmailAddress = userInput.EmailAddress,
                PasswordHash = HashPassword(userInput.PasswordHash),
                CreatedAt = DateTime.UtcNow,
                IsSystemProtected = false,
                IsActive = false,
                Role = UserRole.RegularUser, 
                ProfilePhoto = "/uploads/profile-pictures/defult_user.webp"
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
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(100)
            });

            return CreateResponse("Registration successful. Please check your email for the verification code.", "success");
        }

        // Verify Account endpoint
        [HttpPost("Verify-account")]
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccountInput input)
        {
            if (!Request.Cookies.TryGetValue("EmailForVerification", out var emailAddressFromCookies))
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

            user.IsActive = true;
            user.AccountVerification.CheckedOK = true;
            await _context.SaveChangesAsync();

            Response.Cookies.Delete("EmailForVerification");

            return CreateResponse("Account verification successful. You can now sign in.", "success");
        }

        // Resend Verification Code endpoint
        [HttpPost("Resend-verification-code")]
        public async Task<IActionResult> ResendVerificationCode()
        {

            if (!Request.Cookies.TryGetValue("EmailForVerification", out string emailAddressFromCookies))
            {
                return BadRequest("Verification email not found.");
            }


            var user = await _context.UsersT.Include(u => u.AccountVerification)
                                    .SingleOrDefaultAsync(u => u.EmailAddress == emailAddressFromCookies);

            if (user == null || user.AccountVerification == null)
                return NotFound("User not found.");

            var timeSinceLast = DateTime.UtcNow - user.AccountVerification.Date;
            if (timeSinceLast.TotalMinutes < 2)
            {
                return BadRequest("Please wait at least 2 minutes before resending the code.");
            }


            var newCode = GenerateVerificationCode();
            user.AccountVerification.Code = newCode;
            user.AccountVerification.Date = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _emailQueueService.QueueResendEmail(user.EmailAddress, user.FullName, newCode);

            return Ok("Verification code resent successfully.");
        }

        [HttpPost("Signin")]
        public async Task<IActionResult> Signin([FromBody] UserSignInInput userSignInInput)
        {
            if (!ModelState.IsValid)
                return Unauthorized(new { message = "Invalid login credentials." });

            string email = userSignInInput.Email;

            // 1️ Check lockout due to previous failed attempts
            var failedAttempts = _failedLoginTracker.GetFailedAttempts();
            if (failedAttempts.ContainsKey(email) && failedAttempts[email].LockoutEnd > DateTime.UtcNow)
            {
                var remaining = failedAttempts[email].LockoutEnd - DateTime.UtcNow;
                return BadRequest($"Too many failed attempts. Try again after {remaining:mm\\:ss} minutes.");
            }

            // 2️ Retrieve user
            var existingUser = await _context.UsersT
                .Include(u => u.AccountVerification)
                .SingleOrDefaultAsync(x => x.EmailAddress == email);

            if (existingUser == null || !VerifyPassword(userSignInInput.Password, existingUser.PasswordHash))
            {
                _failedLoginTracker.RecordFailedAttempt(email);

                if (failedAttempts.TryGetValue(email, out var attemptData) && attemptData.Attempts >= 5)
                {
                    _failedLoginTracker.LockUser(email);
                    return BadRequest("Too many failed login attempts. You are locked out for 15 minutes.");
                }

                return BadRequest("Invalid login credentials.");
            }

            // 3️ Check account verification
            if (existingUser.AccountVerification != null && !existingUser.AccountVerification.CheckedOK)
            {
                var newVerificationCode = GenerateVerificationCode();
                existingUser.AccountVerification.Code = newVerificationCode;
                existingUser.AccountVerification.Date = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _emailQueueService.QueueResendEmail(existingUser.EmailAddress, existingUser.FullName, newVerificationCode);

                return BadRequest("Your account is not verified. A new verification code has been sent to your email.");
            }

            // 4️ Check if account is deleted or deactivated
            if (existingUser.IsDeleted)
                return BadRequest(new { message = "This account has been deleted. Please contact support." });

            if (!existingUser.IsActive)
                return BadRequest(new { message = "Your account is not activated." });

            // 5️ Reset failed attempts after successful login
            _failedLoginTracker.ResetFailedAttempts(email);

            // 6️ Register User Visit
            _context.UserVisitHistoryT.Add(new UserVisitHistory
            {
                UserId = existingUser.UserId,
                LastVisit = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // 7️ Generate JWT and Refresh Token
            var tokenDuration = userSignInInput.RememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(3);

            var jwtToken = GenerateAccessToken(
                existingUser.UserId.ToString(),
                existingUser.EmailAddress,
                existingUser.FullName,
                existingUser.Role,
                tokenDuration
            );

            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                UserId = existingUser.UserId
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // 8️ Set Remember Me Cookies if requested
            if (userSignInInput.RememberMe)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(30)
                };
                Response.Cookies.Append("UserEmail", existingUser.EmailAddress, cookieOptions);
                Response.Cookies.Append("UserPassword", userSignInInput.Password, cookieOptions);
            }

            // 9️ Return Response
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                expired = jwtToken.ValidTo,
                role = existingUser.Role.ToString(),
                refreshToken = refreshToken.Token
            });
        }


        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] string oldRefreshToken)
        {
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == oldRefreshToken && !rt.IsRevoked);

            if (refreshToken == null || refreshToken.ExpiryDate < DateTime.UtcNow)
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            //Old Token Heroes
            refreshToken.IsRevoked = true;

            var newAccessToken = GenerateAccessToken(
                refreshToken.User.UserId.ToString(),
                refreshToken.User.EmailAddress,
                refreshToken.User.FullName,
                refreshToken.User.Role
            );

            //Create a new RefreshToken
            var newRefreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                UserId = refreshToken.User.UserId
            };

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                expired = newAccessToken.ValidTo,
                refreshToken = newRefreshToken.Token
            });
        }

        [HttpGet("auto-login")]
        public async Task<IActionResult> AutoLoginFromCookies()
        {
            var email = Request.Cookies["UserEmail"];
            var password = Request.Cookies["UserPassword"];

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return Unauthorized(new { message = "Auto-login failed: missing cookies." });
            }

            var existingUser = await _context.UsersT
                .Include(u => u.AccountVerification)
                .SingleOrDefaultAsync(x => x.EmailAddress == email && !x.IsDeleted);

            if (existingUser == null || !VerifyPassword(password, existingUser.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            // Check if the account is verified
            if (existingUser.AccountVerification != null && !existingUser.AccountVerification.CheckedOK)
            {
                return BadRequest("Your account is not verified. Please check your email for the verification code.");
            }

            if (existingUser.IsDeleted == true)
            {
                return BadRequest(new { message = "This account has been deleted. Please contact support." });

            }

            if (existingUser.IsActive == false)
            {
                return BadRequest(new { message = "Your account is not activated" });
            }

            JwtSecurityToken mytoken = GenerateAccessToken(
                existingUser.UserId.ToString(),
                existingUser.EmailAddress,
                existingUser.FullName,
                existingUser.Role
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(mytoken),
                expired = mytoken.ValidTo,
                role = existingUser.Role.ToString()
            });

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

                var resetLink = $"https://yourfrontend.com/reset-password?email={user.EmailAddress}&code={verificationCode}";

                _emailQueueService.QueueEmail(user.EmailAddress, user.FullName, verificationCode, resetLink);

                return Ok("A verification code has been sent to your email address.");
            }

            return BadRequest(ModelState);
        }

        // Reset Password endpoint
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.UsersT
                .Include(u => u.AccountVerification)
                .FirstOrDefaultAsync(u => u.EmailAddress == input.Email);

            if (user == null || user.AccountVerification == null)
                return NotFound(new { message = "Invalid email or verification code." });

            if (user.AccountVerification.Code != input.Code ||
                user.AccountVerification.Date.AddMinutes(30) < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Invalid or expired verification code." });
            }

            user.PasswordHash = HashPassword(input.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successful. You can now log in." });
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

                var tokenHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrWhiteSpace(tokenHeader) || !tokenHeader.StartsWith("Bearer "))
                {
                    return BadRequest(new { message = "Invalid token format." });
                }

                var token = tokenHeader.Replace("Bearer ", "").Trim();

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

        // Generating token based on user information
        private JwtSecurityToken GenerateAccessToken(string userId, string email, string fullName, UserRole role, TimeSpan? customExpiry = null)
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
                expires: DateTime.UtcNow.Add(customExpiry ?? TimeSpan.FromHours(1)),
                signingCredentials: signingCredentials
            );

            return token;
        }

        // Signin endpoint
        private static Dictionary<string, (int Attempts, DateTime LockoutEnd)> _failedLoginAttempts = new();

        // Verify a password against the stored hash
        private bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash) || !storedHash.Contains(":"))
                return false;

            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;

            if (!Convert.TryFromBase64String(parts[0], new byte[16], out _))
                return false; // Checking that 'Salt' is true

            if (!Convert.TryFromBase64String(parts[1], new byte[32], out _))
                return false; // Checking that 'Hash' is true

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHashBytes = Convert.FromBase64String(parts[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(storedHashBytes.Length);

            return CryptographicOperations.FixedTimeEquals(hash, storedHashBytes);
        }
        private string GenerateVerificationCode()
        {
            byte[] bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            int code = BitConverter.ToInt32(bytes, 0) % 900000 + 100000; 
            return Math.Abs(code).ToString();
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
            byte[] salt = new byte[16];

            RandomNumberGenerator.Fill(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

    }

}
