using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System;

namespace GraduationProjectBackendAPI.Controllers.User
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public IActionResult GetAdminDashboard()
        {
            return Ok(new { message = "Welcome to Admin Dashboard!" });
        }

        [HttpGet("all-users")]
        public IActionResult GetAllUsers()
        {
            var allUsers = _context.UsersT.Where(u => u.Role == UserRole.RegularUser).ToList();
            return Ok(new {  allUsers } );
        }

        [HttpGet("all-instructors")]
        public IActionResult GetAllInstructors()
        {
            var allInstructors = _context.UsersT.Where(u => u.Role == UserRole.Instructor).ToList();

            return Ok(new { allInstructors });
        }

        [HttpGet("all-admins")]
        public IActionResult GetAllAdmins()
        {
            var allAdmins = _context.UsersT.Where(u => u.Role == UserRole.Admin).ToList();
            return Ok(new { allAdmins });
        }

        [HttpPost("make-instructor/{userId}")]
        public async Task <IActionResult> MakeInstructor(int userId)
        {
            var adminId = GetUserIdFromToken();
            if (adminId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await FindUserById(userId);
            if (user == null)
                return NotFound(new { message = "User not found!" });

            user.Role = Models.User.UserRole.Instructor;
            await _context.SaveChangesAsync();

            LogAdminAction(adminId.Value, userId, "MakeInstructor", $"User {user.EmailAddress} promoted to Instructor");

            return Ok(new { message = "User promoted to Instructor successfully.", user });
        }

        [HttpPost("make-admin/{userId}")]
        public async Task<IActionResult> MakeAdmin(int userId)
        {
            var adminId = GetUserIdFromToken();
            if (adminId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await FindUserById(userId);
            if (user == null)
                return NotFound(new { message = "User not found!" });

            user.Role = Models.User.UserRole.Admin;
            await _context.SaveChangesAsync();

            LogAdminAction(adminId.Value, userId, "MakeAdmin", $"User {user.EmailAddress} promoted to Admin");

            return Ok(new { message = "User promoted to Admin successfully.", user });
        }

        [HttpPost("make-regular-user/{userId}")]
        public async Task<IActionResult> MakeRegularUser(int userId)
        {
            var adminId = GetUserIdFromToken();
            if (adminId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await FindUserById(userId);
            if (user == null)
                return NotFound(new { message = "User not found!" });

            user.Role = Models.User.UserRole.RegularUser;
            await _context.SaveChangesAsync();

            LogAdminAction(adminId.Value, userId, "MakeRegularUser", $"User {user.EmailAddress} demoted to Regular User");

            return Ok(new { message = "User demoted to Regular User successfully.", user });
        }

        [HttpGet("search-user")]
        public IActionResult SearchUserByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email is required for search!" });

            var user = _context.UsersT
                        .FirstOrDefault(u => u.EmailAddress.ToLower() == email.ToLower());

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new { user });
        }

        private async Task<Users> FindUserById(int userId)
        {
            return await _context.UsersT.FindAsync(userId);
        }

        private void LogAdminAction(int adminId, int targetUserId, string actionType, string details)
        {
            var log = new AdminActionLog
            {
                AdminId = adminId,
                TargetUserId = targetUserId,
                ActionType = actionType,
                ActionDetails = details
            };

            _context.adminActionLogs.Add(log);
            _context.SaveChanges();
        }

        [Authorize]
        [HttpGet("get-user-info")]
        public async Task<IActionResult> GetInfoFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await FindUserById(currentUserId);
            if (user == null)
                return NotFound(new { message = "User not found!" });

            return Ok(new { message = "User retrieved successfully!", user });
        }

        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return null;

            return userId;
        }

    }
}
