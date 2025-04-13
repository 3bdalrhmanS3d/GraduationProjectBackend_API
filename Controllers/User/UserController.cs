using GraduationProjectBackendAPI.Models;
using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.Courses;
using GraduationProjectBackendAPI.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GraduationProjectBackendAPI.Controllers.DOT.User;

namespace GraduationProjectBackendAPI.Controllers.User
{
    [Route("api/user")]
    [ApiController]
    [Authorize(Roles = "RegularUser, Instructor, Admin")]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public IActionResult GetUserDashboard()
        {
            return Ok(new { message = "Welcome to User Dashboard!" });
        }

        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileUpdateModel model)
        {
            var userId = GetUserIdFromToken();
            if (userId == null) {
                return Unauthorized(new { message = "Invalid or missing token." });
            }

            var user = await _context.UsersT.FindAsync(userId.Value);
            if (user == null)
                return NotFound(new { message = "User not found!" });

            var userDetails = await _context.DetailsT.FindAsync(userId.Value);

            if (userDetails == null)
            {
                userDetails = new UserDetails
                {
                    UserId = userId.Value,
                    BirthDate = model.BirthDate,
                    Edu = model.Edu,
                    National = model.National,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DetailsT.Add(userDetails);
            }
            else
            {
                userDetails.BirthDate = model.BirthDate;
                userDetails.Edu = model.Edu;
                userDetails.National = model.National;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "User profile updated successfully!", userDetails });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await _context.UsersT
                .Include(u => u.UserDetails)
                .Include(u => u.UserProgresses)
                .ThenInclude(up => up.Course)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
                return NotFound(new { message = "User not found!" });

            if (user.UserDetails == null)
            {
                return BadRequest(new
                {
                    message = "User profile is incomplete. Please complete your profile first.",
                    requiredFields = new { BirthDate = "YYYY-MM-DD", Edu = "Education Level", National = "Nationality" }
                });
            }

            var userProgress = user.UserProgresses.Select(up => new
            {
                up.CourseId,
                CourseName = up.Course?.CourseName,
                up.CurrentLevelId,
                up.CurrentSectionId,
                up.LastUpdated
            }).ToList();

            var userProfile = new
            {
                user.FullName,
                user.EmailAddress,
                user.Role,
                user.ProfilePhoto,
                user.CreatedAt,
                user.UserDetails?.BirthDate,
                user.UserDetails?.Edu,
                user.UserDetails?.National,
                Progress = userProgress
            };

            return Ok(userProfile);
        }

        [HttpPost("pay-course")]
        public async Task<IActionResult> PayForCourse([FromBody] PaymentRequestModel model)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var course = await _context.Courses.FindAsync(model.CourseId);
            if (course == null)
                return NotFound(new { message = "Course not found!" });

            // Ensure that there is no previous successful payment for this session
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.CourseId == model.CourseId && p.Status == PaymentStatus.Completed);

            if (existingPayment != null)
                return BadRequest(new { message = "You have already paid for this course." });

            // New payment registration with 'Pending' status
            var payment = new Payment
            {
                UserId = userId.Value,
                CourseId = model.CourseId,
                Amount = model.Amount,
                PaymentDate = DateTime.UtcNow,
                Status = PaymentStatus.Pending, // Default Status Waiting for confirmation
                TransactionId = model.TransactionId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment recorded successfully. Awaiting confirmation.", payment });
        }

        [HttpPost("confirm-payment/{paymentId}")]
        public async Task<IActionResult> ConfirmPayment(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
                return NotFound(new { message = "Payment record not found!" });

            if (payment.Status == PaymentStatus.Completed)
                return BadRequest(new { message = "Payment already completed!" });

            // Update payment status to 'Completed'

            payment.Status = PaymentStatus.Completed;
            await _context.SaveChangesAsync();

            //User registration automatically in cycle after payment
            var courseEnrollment = new CourseEnrollment
            {
                UserId = payment.UserId,
                CourseId = payment.CourseId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.CourseEnrollments.Add(courseEnrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment confirmed. Course enrollment successful!", payment });
        }

        /*
         * Whether or not the user has completed payment for these courses is verified.
         */
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyCourses()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var myCourses = await _context.CourseEnrollments
                .Where(x => x.UserId == userId.Value)
                .Include(x => x.Course)
                .Join(_context.Payments,
                      enrollment => new { enrollment.UserId, enrollment.CourseId },
                      payment => new { payment.UserId, payment.CourseId },
                      (enrollment, payment) => new { enrollment, payment })
                .Where(joined => joined.payment.Status == PaymentStatus.Completed) 
                .Select(joined => new
                {
                    joined.enrollment.Course.CourseId,
                    joined.enrollment.Course.CourseName,
                    joined.enrollment.Course.Description,
                    joined.enrollment.EnrolledAt
                })
                .ToListAsync();

            if (!myCourses.Any())
                return NotFound(new { message = "You are not enrolled in any paid courses." });

            return Ok(new { count = myCourses.Count, courses = myCourses });
        }


        [HttpGet("favorite-course")]
        public async Task<IActionResult> GetFavoriteCourse()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var favoriteCourses = await _context.FavoriteCourses
                    .Where(f => f.UserId == userId.Value)  
                    .Include(f => f.Course)
                    .ToListAsync();

            if (!favoriteCourses.Any())
                return NotFound(new { message = "No favorite courses found." });

            var courses = favoriteCourses.Select(f => new
            {
                f.Course.CourseId,
                f.Course.CourseName,
                f.Course.Description,
                f.AddedAt
            });

            return Ok(new { count = courses.Count(), courses });

        }

        [HttpPost("upload-profile-photo")]
        public async Task<IActionResult> UploadProfilePhoto( IFormFile file)
        {
            var userId = GetUserIdFromToken();
            if( userId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await _context.UsersT.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if(file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profile-pictures");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            
            // Generate Unique File Name
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"user_{userId}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save File to Server
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update User's Profile Photo in Database
            user.ProfilePhoto = $"/uploads/profile-pictures/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile photo uploaded successfully.", profilePhotoUrl = user.ProfilePhoto });
        }

        [HttpDelete("delete-profile-photo")]
        public async Task<IActionResult> DeleteProfilePhoto()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await _context.UsersT.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            // Check if the user has a custom profile photo
            if (string.IsNullOrEmpty(user.ProfilePhoto) || user.ProfilePhoto == "/uploads/profile-pictures/default.png")
            {
                return BadRequest(new { message = "No custom profile photo to delete." });
            }

            // Define the file path
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(uploadsFolder, user.ProfilePhoto.TrimStart('/')); // Convert URL path to server path

            // Delete the file from the server if it exists
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Reset the profile photo to the default image
            user.ProfilePhoto = "/uploads/profile-pictures/default.png";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile photo deleted successfully. Default photo restored.", profilePhotoUrl = user.ProfilePhoto });
        }

        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return null;

            return userId;
        }
        
    }
    
}
