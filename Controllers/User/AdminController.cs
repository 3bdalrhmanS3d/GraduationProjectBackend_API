using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System;
using Microsoft.EntityFrameworkCore;

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

        // GET /api/admin/all-users?role=RegularUser
        // GET /api/admin/all-users?role=Instructor
        // GET /api/admin/all-users?role=Admin
        // GET /api/admin/all-users

        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserRole? role = null)
        {
            IQueryable<Users> query = _context.UsersT;

            if (role.HasValue)
                query = query.Where(u => u.Role == role.Value);

            var allUsers = await query.ToListAsync();

            return Ok(new { count = allUsers.Count, allUsers });
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
        public async Task<IActionResult> SearchUserByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email is required for search!" });

            var user = await _context.UsersT
                        .FirstOrDefaultAsync(u => string.Equals(u.EmailAddress, email, StringComparison.OrdinalIgnoreCase));

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new { user });
        }


        private async Task<Users> FindUserById(int userId)
        {
            return await _context.UsersT.FindAsync(userId);
        }

        [HttpGet("all-admin-actions")]
        public async Task<IActionResult> GetAllAdminActions()
        {
            var adminId = GetUserIdFromToken();
            if (adminId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var allActions = await _context.adminActionLogs
                .Include(a => a.Admin)
                .Include(a => a.TargetUser)
                .ToListAsync();

            return Ok(new { allActions });
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
