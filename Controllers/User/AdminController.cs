using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System;
using Microsoft.EntityFrameworkCore;
using GraduationProjectBackendAPI.Controllers.Services;
using GraduationProjectBackendAPI.Controllers.DTO.User;

namespace GraduationProjectBackendAPI.Controllers.User
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FailedLoginTracker _failedLoginTracker;
        private readonly EmailQueueBackgroundService _emailQueueBackgroundService;
        private readonly EmailQueueService _emailQueueService;

        public AdminController(AppDbContext context, FailedLoginTracker failedLoginTracker, EmailQueueBackgroundService emailQueueBackgroundService, EmailQueueService emailQueueService)
        {
            _context = context;
            _failedLoginTracker = failedLoginTracker;
            _emailQueueBackgroundService = emailQueueBackgroundService;
            _emailQueueService = emailQueueService;
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
             .Where(u => !u.IsDeleted)
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
                .Where(u => !u.IsDeleted)
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

        [HttpGet("filter-users")]
        public async Task<IActionResult> FilterUsers(
            [FromQuery] string? name,
            [FromQuery] string? email,
            [FromQuery] UserRole? role = null,
            [FromQuery] string? orderBy = null)
        {
            var query = _context.UsersT
                .Where(u => !u.IsDeleted)
                .Include(u => u.AccountVerification)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(u => u.FullName.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(u => u.EmailAddress.Contains(email));
            }
            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }
            query = orderBy?.ToLower() switch
            {
                "newest" => query.OrderByDescending(u => u.CreatedAt),
                "oldest" => query.OrderBy(u => u.CreatedAt),
                "nameasc" => query.OrderBy(u => u.FullName),
                "namedesc" => query.OrderByDescending(u => u.FullName),
                _ => query.OrderByDescending(u => u.CreatedAt) // Default to newest
            };

            var users = await query
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.EmailAddress,
                    u.Role,
                    IsVerified = u.AccountVerification != null && u.AccountVerification.CheckedOK
                })
                .ToListAsync();

            return Ok(new
            {
                Count = users.Count,
                Users = users
            });
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

        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> SoftDeleteUser(int id)
        {
            var adminId = GetUserIdFromToken();
            if (adminId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await FindUserById(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.IsSystemProtected)
                return BadRequest(new { message = "Cannot delete system protected user." });

            if (adminId == id)
                return BadRequest(new { message = "You cannot delete your own account." });

            user.IsDeleted = true;
            await _context.SaveChangesAsync();

            LogAdminAction(adminId.Value, id, "SoftDeleteUser", $"User {user.EmailAddress} marked as deleted");

            return Ok(new { message = "User soft-deleted successfully." });
        }

        [HttpPost("recover-user/{id}")]
        public async Task<IActionResult> RecoverUser(int id)
        {
            var adminId = GetUserIdFromToken();
            if (adminId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await FindUserById(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (!user.IsDeleted)
                return BadRequest(new { message = "User is not deleted." });

            user.IsDeleted = false;
            await _context.SaveChangesAsync();

            LogAdminAction(adminId.Value, id, "RecoverUser", $"User {user.EmailAddress} recovered");

            return Ok(new { message = "User recovered successfully." });
        }

        [HttpGet("system-stats")]
        public async Task<IActionResult> GetSystemStatistics()
        {
            var totalUsers = await _context.UsersT.CountAsync(u => (u.Role == UserRole.RegularUser) && u.IsDeleted == false);

            var activatedUsers = await _context.UsersT.Include(u => u.AccountVerification)
                                         .CountAsync(u => u.AccountVerification != null && u.AccountVerification.CheckedOK);
            var totalCoursesActiva = await _context.Courses.CountAsync(u=>u.IsActive);

            var totalInstructors = await _context.UsersT.CountAsync(u => u.Role == UserRole.Instructor && u.IsDeleted == false);
            var totalAdmins = await _context.UsersT.CountAsync(u => u.Role == UserRole.Admin && u.IsDeleted == false);

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

        [HttpGet("get-history-user")]
        public async Task<IActionResult> GetHistoryUser(int userId)
        {
            var adminId = GetUserIdFromToken();
            if (adminId == null) return Unauthorized();

            var user = await FindUserById(userId);
            if (user == null) return NotFound(new { message = "User not found" });

            var result = await _context.UserVisitHistoryT
                .Where(l => l.UserId == userId)
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost("toggle-user-activation/{userId}")]
        public async Task<IActionResult> ToggleUserActivation(int userId)
        {
            var adminId = GetUserIdFromToken();
            if (adminId == null) return Unauthorized();

            var user = await FindUserById(userId);
            if (user == null) return NotFound(new { message = "User not found" });

            if (user.IsSystemProtected)
                return BadRequest(new { message = "Cannot modify system protected user." });

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            LogAdminAction(adminId.Value, userId, "ToggleUserActivation", $"User {user.EmailAddress} activation toggled");

            return Ok(new { message = $"User activation is now {(user.IsActive ? "enabled" : "disabled")}" });
        }

        [HttpGet("failed-logins")]
        public IActionResult GetFailedLoginAttempts()
        {
            var failedAttempts = _failedLoginTracker.GetFailedAttempts();

            var report = failedAttempts.Select(entry => new
            {
                Email = entry.Key,
                Attempts = entry.Value.Attempts,
                IsCurrentlyLocked = entry.Value.LockoutEnd > DateTime.UtcNow,
                TimeRemainingLockout = entry.Value.LockoutEnd > DateTime.UtcNow
                    ? (entry.Value.LockoutEnd - DateTime.UtcNow).TotalSeconds
                    : 0
            }).ToList();

            return Ok(new
            {
                TotalFailedAccounts = report.Count,
                FailedLoginReport = report
            });
        }

        [HttpPost("send-notification")]
        public async Task<IActionResult> SendNotification([FromBody] AdminSendNotificationInput request)
        {
            var adminId = GetUserIdFromToken();
            if (adminId == null) return Unauthorized();

            var user = await _context.UsersT.FindAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            string subject;
            string bodyMessage;

            if (request.TemplateType.HasValue)
            {
                (subject, bodyMessage) = GetTemplateMessage(request.TemplateType.Value, user.FullName);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { message = "Custom subject and message are required if no template is selected." });
                }

                subject = request.Subject;
                bodyMessage = request.Message;
            }

            await _emailQueueService.SendCustomEmailAsync(user.EmailAddress, user.FullName, subject, bodyMessage);
            await CreateNotification(user.UserId, subject + " - " + bodyMessage);


            LogAdminAction(adminId.Value, user.UserId, "SendNotification", $"Notification sent to {user.EmailAddress}");

            return Ok(new { message = "Notification sent successfully." });
        }

        private (string subject, string bodyMessage) GetTemplateMessage(NotificationTemplateType template, string fullName)
        {
            switch (template)
            {
                case NotificationTemplateType.AccountActivated:
                    return ("✅ Your Account has been Activated", $"Hello {fullName},\n\nYour account has been successfully activated. You can now login and enjoy our services.");
                case NotificationTemplateType.AccountDeactivated:
                    return ("⚠️ Account Deactivated", $"Hello {fullName},\n\nYour account has been temporarily deactivated. Please contact support for further information.");
                case NotificationTemplateType.AccountDeleted:
                    return ("🗑️ Account Deleted", $"Hello {fullName},\n\nYour account has been deleted from our platform. If this was a mistake, please contact support immediately.");
                case NotificationTemplateType.AccountRestored:
                    return ("♻️ Account Restored", $"Hello {fullName},\n\nGood news! Your account has been restored. Welcome back!");
                case NotificationTemplateType.GeneralAnnouncement:
                    return ("📢 Important Announcement", $"Hello {fullName},\n\nWe have an important update for you. Please check your dashboard for more information.");
                default:
                    return ("📬 Notification", $"Hello {fullName},\n\nThis is a notification from the administration.");
            }
        }

        private async Task CreateNotification(int userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationT.Add(notification);
            await _context.SaveChangesAsync();
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
