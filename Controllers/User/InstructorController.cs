using GraduationProjectBackendAPI.Controllers.DOT.Courses;
using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace GraduationProjectBackendAPI.Controllers.User
{
    [Route("api/instructor")]
    [ApiController]
    [Authorize(Roles = "Instructor, Admin")]
    public class InstructorController : Controller
    {
        // GET: api/instructor
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly EmailQueueService _emailQueueService;

        public InstructorController(AppDbContext context, IConfiguration config, EmailQueueService emailQueueService)
        {
            _context = context;
            _config = config;
            _emailQueueService = emailQueueService;
        }

        [HttpGet("dashboard")]
        public IActionResult GetInstructorDashboard()
        {
            return Ok(new { message = "Welcome to Instructor Dashboard!" });
        }

        // Track
        // course 
        // level
        // section

        #region Cousre

        // Get all courses for this instructor
        [HttpGet("all-courses-for-me")]
        public async Task<IActionResult> GetAllCourses()
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null)
                return Unauthorized(new { message = "Invalid or missing token." });


            var user = await _context.UsersT.FindAsync(instructorId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            var courses = _context.Courses
                .Where(c => c.InstructorId == user.UserId)
                .ToList();

            if (courses == null || !courses.Any())
            {
                return NotFound(new { message = "No courses found" });
            }

            return Ok(new { message = "Here are all the courses for you!", courses });

        }

        // Get course by id
        [HttpGet("get-course-details/{courseId}")]
        public async Task<IActionResult> GetCourseDetails(int courseId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var course = await _context.Courses
                .Include(c => c.aboutCourses)
                .Include(c => c.CourseSkills)
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);

            if (course == null)
                return NotFound(new { message = "Course not found or not owned by you." });

            return Ok(new
            {
                course.CourseId,
                course.CourseName,
                course.Description,
                course.CourseImage,
                course.CoursePrice,
                course.IsActive,
                Abouts = course.aboutCourses!.Select(a => new { a.AboutCourseId, a.AboutCourseText, a.outcametype }),
                Skills = course.CourseSkills!.Select(s => new { s.CourseSkillId, s.CourseSkillText })
            });

        }

        // create course
        [HttpPost("create-course")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseInput input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            if(!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data." , ModelState });
            }

            if (input.CoursePrice < 0)
                return BadRequest(new { message = "Course price cannot be negative." });

            var newCourse = new Courses
            {
                CourseName = input.CourseName,
                Description = input.Description,
                CoursePrice = input.CoursePrice,
                CourseImage = string.IsNullOrWhiteSpace(input.CourseImage) ? "/uploads/courses/default.jpg" : input.CourseImage,
                InstructorId = instructorId.Value,
                CreatedAt = DateTime.UtcNow,
                IsActive = input.IsActive, // Set the course as active or inactive based on the input, default is false
            };

            _context.Courses.Add(newCourse);
            await _context.SaveChangesAsync();

            // Add about course texts
            if (input.AboutCourseTexts != null && input.AboutCourseTexts.Any())
            {
                foreach (var text in input.AboutCourseTexts)
                {
                    var aboutCourse = new AboutCourse
                    {
                        CourseId = newCourse.CourseId,
                        AboutCourseText = FormatText(text.AboutCourseText),
                        outcametype = Outcame.learn,

                    };
                    _context.aboutCourses.Add(aboutCourse);
                }
                
            }
            // Add course skills
            if (input.CourseSkills != null && input.CourseSkills.Any())
            {
                foreach (var skill in input.CourseSkills)
                {
                    var courseSkill = new CourseSkill
                    {
                        CourseId = newCourse.CourseId,
                        CourseSkillText = FormatText(skill.CourseSkillText),
                    };

                    _context.courseSkills.Add(courseSkill);
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Course created successfully!", course = newCourse });

        }

        // Update course 
        [HttpPut("update-course/{courseId}")]
        public async Task<IActionResult> UpdateCourse(int courseId, [FromBody] CreateCourseInput input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var course = await _context.Courses
                .Include(c => c.aboutCourses)
                .Include(c => c.CourseSkills)
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);

            if (course == null)
                return NotFound(new { message = "Course not found or you are not the owner." });

            // تحديث البيانات الأساسية
            if (!string.IsNullOrWhiteSpace(input.CourseName))
                course.CourseName = input.CourseName;

            if (!string.IsNullOrWhiteSpace(input.Description))
                course.Description = input.Description;

            if (input.CoursePrice > 0)
                course.CoursePrice = input.CoursePrice;

            if (!string.IsNullOrWhiteSpace(input.CourseImage))
                course.CourseImage = input.CourseImage;

            // تحديث AboutCourse
            if (input.AboutCourseTexts != null)
            {
                var updatedIds = input.AboutCourseTexts.Select(a => a.AboutCourseId).ToList();

                var existing = course.aboutCourses ?? new List<AboutCourse>();

                // حذف الغير موجود في الطلب
                var toDelete = existing.Where(a => !updatedIds.Contains(a.AboutCourseId)).ToList();
                _context.aboutCourses.RemoveRange(toDelete);

                foreach (var aboutInput in input.AboutCourseTexts)
                {
                    if (aboutInput.AboutCourseId == 0)
                    {
                        _context.aboutCourses.Add(new AboutCourse
                        {
                            CourseId = course.CourseId,
                            AboutCourseText = FormatText(aboutInput.AboutCourseText),
                            outcametype = aboutInput.OutcameType
                        });
                    }
                    else
                    {
                        var existingAbout = existing.FirstOrDefault(a => a.AboutCourseId == aboutInput.AboutCourseId);
                        if (existingAbout != null)
                        {
                            existingAbout.AboutCourseText = FormatText(aboutInput.AboutCourseText);
                            existingAbout.outcametype = aboutInput.OutcameType;
                        }
                    }
                }
            }

            // تحديث CourseSkills
            if (input.CourseSkills != null)
            {
                var updatedSkillIds = input.CourseSkills.Select(s => s.CourseSkillId).ToList();

                var existingSkills = course.CourseSkills ?? new List<CourseSkill>();

                var toDeleteSkills = existingSkills.Where(s => !updatedSkillIds.Contains(s.CourseSkillId)).ToList();
                _context.courseSkills.RemoveRange(toDeleteSkills);

                foreach (var skillInput in input.CourseSkills)
                {
                    if (skillInput.CourseSkillId == 0)
                    {
                        _context.courseSkills.Add(new CourseSkill
                        {
                            CourseId = course.CourseId,
                            CourseSkillText = FormatText(skillInput.CourseSkillText)
                        });
                    }
                    else
                    {
                        var existingSkill = existingSkills.FirstOrDefault(s => s.CourseSkillId == skillInput.CourseSkillId);
                        if (existingSkill != null)
                        {
                            existingSkill.CourseSkillText = FormatText(skillInput.CourseSkillText);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Course updated successfully!", course });
        }

        // Delete course
        [HttpDelete("delete-course/{courseId}")]
        public async Task<IActionResult> DeleteCourse(int courseId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null)
                return Unauthorized();

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);

            if (course == null)
                return NotFound(new { message = "Course not found or not owned by you." });

            course.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course soft-deleted successfully." });
        }

        // Toggle Course Status
        // if active = true => course is active 
        // if active = false => course is inactive

        [HttpPost("toggle-course-status/{courseId}")]
        public async Task<IActionResult> ToggleCourseStatus(int courseId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null)
                return Unauthorized();

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);

            if (course == null)
                return NotFound(new { message = "Course not found or not owned by you." });

            course.IsActive = !course.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = course.IsActive
                    ? "Course is now active and visible to students."
                    : "Course is now inactive and hidden from students.",
                status = course.IsActive ? "active" : "inactive"
            });
        }

        // Upload Course Image 
        [HttpPost("upload-course-image/{courseId}")]
        public async Task<IActionResult> UploadCourseImage(int courseId, IFormFile file)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);

            if (course == null) return NotFound(new { message = "Course not found or not yours." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/CoursesImages");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"course_{courseId}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            course.CourseImage = $"/uploads/courses/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course image uploaded successfully.", course.CourseImage });
        }


        #endregion



        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return null;

            return userId;
        }

        // Format the skill text
        private static string FormatText(string skill)
        {
            if (string.IsNullOrWhiteSpace(skill))
                return string.Empty;

            // Remove extra spaces
            skill = skill.Trim();                          // Trim spaces at ends
            skill = System.Text.RegularExpressions.Regex.Replace(skill, @"\s+", " "); // Remove inner double spaces

            // Capitalize first letter
            if (skill.Length == 1)
                return skill.ToUpper();

            return char.ToUpper(skill[0]) + skill.Substring(1).ToLower();
        }


    }
}
