using GraduationProjectBackendAPI.Controllers.DOT.User;
using GraduationProjectBackendAPI.Models;
using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            if (userId == null)
            {
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
        public async Task<IActionResult> UploadProfilePhoto(IFormFile file)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var user = await _context.UsersT.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (file == null || file.Length == 0)
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



        // For user with course

        [HttpGet("all-tracks")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTracks()
        {
            var tracks = await _context.CourseTracks
                .Include(t => t.CourseTrackCourses)!
                .ThenInclude(ctc => ctc.Courses)
                .ToListAsync();

            var result = tracks.Select(track => new
            {
                track.TrackId,
                track.TrackName,
                track.TrackDescription,
                CourseCount = track.CourseTrackCourses!
                    .Count(c => !c.Courses.IsDeleted && c.Courses.IsActive)
            });
            if (!result.Any())
                return NotFound(new { message = "No tracks found." });

            return Ok(new
            {
                totalTracks = result.Count(),
                tracks = result
            });
        }

        [HttpGet("track-courses/{trackId}")]
        [AllowAnonymous] // or [Authorize] if needed
        public async Task<IActionResult> GetCoursesInTrack(int trackId)
        {
            var track = await _context.CourseTracks
                .Include(t => t.CourseTrackCourses)
                    .ThenInclude(ctc => ctc.Courses)
                        .ThenInclude(c => c.User)
                .Include(t => t.CourseTrackCourses)
                    .ThenInclude(ctc => ctc.Courses)
                        .ThenInclude(c => c.Levels)
                .FirstOrDefaultAsync(t => t.TrackId == trackId);

            if (track == null)
                return NotFound(new { message = "Track not found." });

            var courses = track.CourseTrackCourses
                .Where(ctc => !ctc.Courses.IsDeleted && ctc.Courses.IsActive)
                .Select(ctc => new
                {
                    ctc.Courses.CourseId,
                    ctc.Courses.CourseName,
                    ctc.Courses.Description,
                    ctc.Courses.CourseImage,
                    ctc.Courses.CoursePrice,
                    ctc.Courses.CreatedAt,
                    InstructorName = ctc.Courses.User.FullName,
                    LevelsCount = ctc.Courses.Levels?.Count ?? 0
                }).ToList();

            return Ok(new
            {
                track.TrackId,
                track.TrackName,
                track.TrackDescription,
                totalCourses = courses.Count,
                courses
            });
        }

        // for search
        [HttpGet("all-courses-for-me")]
        public async Task<IActionResult> GetAllCourses([FromQuery] string? search)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            var coursesQuery = _context.Courses
                .Where(c => !c.IsDeleted && c.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                coursesQuery = coursesQuery.Where(c =>
                    c.CourseName.ToLower().Contains(search) ||
                    c.Description.ToLower().Contains(search));
            }

            var courses = await coursesQuery.ToListAsync();

            if (!courses.Any())
                return NotFound(new { message = "No courses found." });


            var result = courses.Select(c => new
            {
                c.CourseId,
                c.CourseName,
                c.Description,
                c.CourseImage,
                c.CoursePrice,
            }
            );
            return Ok(new
            {
                message = "Filtered courses fetched successfully.",
                count = result.Count(),
                courses = result
            });
        }

        [HttpGet("course-levels/{courseId}")]
        public async Task<IActionResult> GetCourseLevels(int courseId)
        {
            var userId = GetUserIdFromToken();
            if (userId == null) return Unauthorized();

            var enrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);

            if (!enrolled)
                return NotFound(new { message = "You are not enrolled in this course." });

            var course = await _context.Courses
                .Include(c => c.Levels!.Where(l => !l.IsDeleted && l.IsVisible).OrderBy(l => l.LevelOrder))
                .FirstOrDefaultAsync(c => c.CourseId == courseId && !c.IsDeleted);

            if (course == null)
                return NotFound(new { message = "Course not found." });

            var levels = course.Levels!.Select(level => new
            {
                level.LevelId,
                level.LevelName,
                level.LevelDetails,
                level.LevelOrder,
                level.IsVisible
            }).ToList();

            if (!levels.Any())
                return NotFound(new { message = "No levels found for this course." });

            return Ok(new
            {
                course.CourseId,
                course.CourseName,
                course.Description,
                course.CourseImage,
                levelsCount = levels.Count,
                levels
            });
        }


        [HttpGet("level-sections/{levelId}")]
        public async Task<IActionResult> GetLevelSections(int levelId)
        {
            var userId = GetUserIdFromToken();
            if (userId == null) return Unauthorized();

            // Get level and its course
            var level = await _context.Levels
                .Include(l => l.Sections!.OrderBy(s => s.SectionOrder))
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LevelId == levelId && !l.IsDeleted);

            if (level == null)
                return NotFound(new { message = "Level not found." });

            // Ensure user is enrolled in the same course
            var enrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == level.Course.CourseId);

            if (!enrolled)
                return NotFound(new { message = "You are not enrolled in this course." });

            // Get user progress if exists
            var userProgress = await _context.UserProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == level.Course.CourseId);

            // Filter and build section list
            var sections = level.Sections!
                .Where(s => !s.IsDeleted && s.IsVisible)
                .OrderBy(s => s.SectionOrder)
                .Select(section => new
                {
                    section.SectionId,
                    section.SectionName,
                    section.SectionOrder,
                    isCurrent = userProgress != null && userProgress.CurrentSectionId == section.SectionId,
                    isCompleted = userProgress != null && section.SectionOrder <
                        level.Sections!.FirstOrDefault(s => s.SectionId == userProgress.CurrentSectionId)?.SectionOrder
                })
                .ToList();

            if (!sections.Any())
                return NotFound(new { message = "No sections found for this level." });

            return Ok(new
            {
                level.LevelId,
                level.LevelName,
                level.LevelDetails,
                sections
            });
        }

        [HttpGet("section-contents/{sectionId}")]
        public async Task<IActionResult> GetSectionContents(int sectionId)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized();

            var section = await _context.Sections
                .Include(s => s.Level)
                .ThenInclude(l => l.Course)
                .Include(s => s.Contents!.Where(c => c.IsVisible).OrderBy(c => c.ContentOrder))
                .FirstOrDefaultAsync(s => s.SectionId == sectionId && !s.IsDeleted && s.IsVisible);

            if (section == null)
                return NotFound(new { message = "Section not found or not accessible." });

            var enrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == section.Level.CourseId);

            if (!enrolled)
                return Forbid("You are not enrolled in this course.");

            var contents = section.Contents!.Select(c => new
            {
                c.ContentId,
                c.Title,
                c.ContentType,
                c.ContentText,
                c.ContentDoc,
                c.ContentUrl,
                c.DurationInMinutes,
                c.ContentDescription
            });
            if (!contents.Any())
                return NotFound(new { message = "No contents found for this section." });

            return Ok(new
            {
                section.SectionId,
                section.SectionName,
                contentsCount = contents.Count(),
                contents
            });
        }

        [HttpPost("complete-section/{currentSectionId}")]
        public async Task<IActionResult> CompleteSection(int currentSectionId)
        {
            var userId = GetUserIdFromToken();
            if (userId == null) return Unauthorized();

            var currentSection = await _context.Sections
                .Include(s => s.Level)
                .FirstOrDefaultAsync(s => s.SectionId == currentSectionId);

            if (currentSection == null)
                return NotFound(new { message = "Section not found." });

            var allSections = await _context.Sections
                .Where(s => s.LevelId == currentSection.LevelId)
                .OrderBy(s => s.SectionOrder)
                .ToListAsync();

            var currentIndex = allSections.FindIndex(s => s.SectionId == currentSectionId);
            var nextSection = currentIndex + 1 < allSections.Count ? allSections[currentIndex + 1] : null;

            var userProgress = await _context.UserProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == currentSection.Level.CourseId);

            if (userProgress == null)
            {
                userProgress = new UserProgress
                {
                    UserId = userId.Value,
                    CourseId = currentSection.Level.CourseId,
                    CurrentLevelId = currentSection.LevelId,
                    CurrentSectionId = nextSection?.SectionId ?? currentSectionId,
                    LastUpdated = DateTime.UtcNow
                };
                _context.UserProgresses.Add(userProgress);
            }
            else
            {
                userProgress.CurrentLevelId = currentSection.LevelId;
                userProgress.CurrentSectionId = nextSection?.SectionId ?? currentSectionId;
                userProgress.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = nextSection != null ? "Moved to next section." : "This was the last section in this level.",
                nextSectionId = nextSection?.SectionId
            });
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
