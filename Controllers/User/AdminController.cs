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
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.UsersT
            .Include(u => u.AccountVerification)
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.EmailAddress,
                u.Role,
                IsVerified = u.AccountVerification != null && u.AccountVerification.CheckedOK
            })
            .ToListAsync();

            var activatedUsers = users.Where(u => u.IsVerified).ToList();
            var notActivatedUsers = users.Where(u => !u.IsVerified).ToList();

            return Ok(new
            {
                ActivatedCount = activatedUsers.Count,
                ActivatedUsers = activatedUsers,
                NotActivatedCount = notActivatedUsers.Count,
                NotActivatedUsers = notActivatedUsers
            });
        }

        [HttpGet("get-basic-user-info/{userId}")]
        public async Task<IActionResult> GetBasicUserInfo(int userId)
        {
            var user = await _context.UsersT
                .Include(u => u.UserDetails)
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Role,
                    u.EmailAddress,
                    Details = u.UserDetails == null ? null : new
                    {
                        u.UserDetails.BirthDate,
                        u.UserDetails.Edu,
                        u.UserDetails.National,
                        u.UserDetails.CreatedAt
                    }
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new { user });
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

            if (user.IsSystemProtected)
                return BadRequest(new { message = "This user is protected and cannot be modified." });

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
            
            if (user.IsSystemProtected)
                return BadRequest(new { message = "This user is protected and cannot be modified." });

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

        [HttpGet]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var adminId = GetUserIdFromToken();
            if(adminId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await FindUserById(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.IsSystemProtected)
                return BadRequest(new { message = "Cannot delete the system protected admin user." });

            if (adminId == id)
                return BadRequest(new { message = "You cannot delete your own account." });

            _context.UsersT.Remove(user);
            await _context.SaveChangesAsync();
            LogAdminAction(adminId.Value, id, "DeleteUser", $"User {user.EmailAddress} deleted");

            return Ok(
                new {
                    message = "User deleted successfully."
                });

        }
        [HttpGet("system-stats")]
        public async Task<IActionResult> GetSystemStatistics()
        {
            var totalUsers = await _context.UsersT.CountAsync(u => u.Role == UserRole.RegularUser);

            var activatedUsers = await _context.UsersT.Include(u => u.AccountVerification)
                                         .CountAsync(u => u.AccountVerification != null && u.AccountVerification.CheckedOK);
            var totalCoursesActiva = await _context.Courses.CountAsync(u=>u.IsActive);

            var totalInstructors = await _context.UsersT.CountAsync(u => u.Role == UserRole.Instructor);
            var totalAdmins = await _context.UsersT.CountAsync(u => u.Role == UserRole.Admin);

            return Ok(new
            {
                TotalUsers = totalUsers,
                ActivatedUsers = activatedUsers,
                NotActivatedUsers = totalUsers - activatedUsers,
                TotalAdmins = totalAdmins,
                TotalInstructors = totalInstructors,
                TotalCourses = totalCoursesActiva
            });
        }

        [HttpGet("admin-logs")]
        public async Task<IActionResult> GetAdminLogs()
        {
            var logs = await _context.adminActionLogs
                .Include(l => l.Admin)
                .Include(l => l.TargetUser)
                .Select(l => new
                {
                    l.LogId,
                    AdminName = l.Admin.FullName,
                    AdminEmail = l.Admin.EmailAddress,
                    TargetUserName = l.TargetUser != null ? l.TargetUser.FullName : null,
                    TargetUserEmail = l.TargetUser != null ? l.TargetUser.EmailAddress : null,
                    l.ActionType,
                    l.ActionDetails,
                    l.ActionDate
                })
                .OrderByDescending(l => l.ActionDate)
                .ToListAsync();

            return Ok(new { Count = logs.Count, Logs = logs });
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
