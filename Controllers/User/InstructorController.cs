using GraduationProjectBackendAPI.Controllers.DOT.Courses;
using GraduationProjectBackendAPI.Controllers.DTO.Courses;
using GraduationProjectBackendAPI.Controllers.Services;
using GraduationProjectBackendAPI.Models.AppDBContext;
using GraduationProjectBackendAPI.Models.Courses;
using GraduationProjectBackendAPI.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        // content
        // quiz
        // task
        // dashboard

        #region Track

        // create track
        [HttpPost("create-track")]
        public async Task<IActionResult> CreateTrack([FromBody] CreateTrackInput input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null)
                return Unauthorized();

            
            if (!ModelState.IsValid)
            {
                var track = new CourseTrack
                {
                    TrackName = input.TrackName.Trim(),
                    TrackImage = input.TrackImage ?? "/uploads/TrackImages/default.png",
                    TrackDescription = input.TrackDescription?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.CourseTracks.Add(track);
                await _context.SaveChangesAsync();


                if (input.CourseIds != null && input.CourseIds.Any())
                {
                    foreach (var courseId in input.CourseIds)
                    {
                        var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
                        if (course != null)
                        {
                            _context.CourseTrackCourses.Add(new CourseTrackCourse
                            {
                                TrackId = track.TrackId,
                                CourseId = course.CourseId
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TrackInstructorActions("Create", $"Track created successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
                }

                return Ok(new { message = "Track created successfully", trackId = track.TrackId });
            }
            else
            {
                return BadRequest(new { message = "Invalid input data.", ModelState });
            }

            
        }

        // add track image
        [HttpPost("upload-track-image/{trackId}")]
        public async Task<IActionResult> UploadTrackImage(int trackId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var track = await _context.CourseTracks.FindAsync(trackId);
            if (track == null)
                return NotFound(new { message = "Track not found." });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/TrackImages");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"track_{trackId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/TrackImages/{fileName}";
            track.TrackImage = relativePath;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Track image uploaded and linked successfully.",
                imageUrl = relativePath
            });
        }

        // update track
        [HttpPut("update-track/{trackId}")]
        public async Task<IActionResult> UpdateTrack(int trackId, [FromBody] UpdateTrackInput input)
        {
            var track = await _context.CourseTracks.FindAsync(trackId);
            if (track == null)
                return NotFound(new { message = "Track not found." });

            track.TrackName = input.TrackName?.Trim() ?? track.TrackName;
            track.TrackDescription = input.TrackDescription?.Trim() ?? track.TrackDescription;

            await _context.SaveChangesAsync();

            TrackInstructorActions("Update", $"Track updated successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Track updated successfully", track });
        }

        // remove course from track
        [HttpDelete("remove-course-from-track")]
        public async Task<IActionResult> RemoveCourseFromTrack([FromQuery] int trackId, [FromQuery] int courseId)
        {
            var entry = await _context.CourseTrackCourses
                .FirstOrDefaultAsync(x => x.TrackId == trackId && x.CourseId == courseId);

            if (entry == null)
                return NotFound(new { message = "Course not found in this track." });

            _context.CourseTrackCourses.Remove(entry);
            await _context.SaveChangesAsync();

            TrackInstructorActions("Remove", $"Course removed from track successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Course removed from track." });
        }

        // delete track
        [HttpDelete("delete-track/{trackId}")]
        public async Task<IActionResult> DeleteTrack(int trackId)
        {
            var track = await _context.CourseTracks
                .Include(t => t.CourseTrackCourses)
                .FirstOrDefaultAsync(t => t.TrackId == trackId);

            if (track == null)
                return NotFound(new { message = "Track not found." });

            _context.CourseTrackCourses.RemoveRange(track.CourseTrackCourses!);
            _context.CourseTracks.Remove(track);
            await _context.SaveChangesAsync();

            TrackInstructorActions("Delete", $"Track deleted successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Track deleted successfully." });
        }


        // add course to track
        [HttpPost("add-course-to-track")]
        public async Task<IActionResult> AddCourseToTrack([FromBody] AddCourseToTrackInput input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null)
                return Unauthorized();

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == input.CourseId && c.InstructorId == instructorId);
            if (course == null)
                return BadRequest(new { message = "Course not found or not yours." });

            var exists = await _context.CourseTrackCourses.AnyAsync(x => x.TrackId == input.TrackId && x.CourseId == input.CourseId);
            if (exists)
                return BadRequest(new { message = "Course already exists in this track." });

            _context.CourseTrackCourses.Add(new CourseTrackCourse
            {
                TrackId = input.TrackId,
                CourseId = input.CourseId
            });

            await _context.SaveChangesAsync();

            TrackInstructorActions("Add", $"Course added to track successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Course added to track." });
        }

        // get all tracks
        [HttpGet("all-tracks")]
        public async Task<IActionResult> GetAllTracks()
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null)
                return Unauthorized();

            var tracks = await _context.CourseTracks
                .Include(t => t.CourseTrackCourses)!
                .ThenInclude(ctc => ctc.Courses)
                .Where(t => t.CourseTrackCourses!.Any(c => c.Courses.InstructorId == instructorId))
                .ToListAsync();

            var result = tracks.Select(track => new
            {
                track.TrackId,
                track.TrackName,
                track.TrackDescription,
                track.CreatedAt,
                Courses = track.CourseTrackCourses!.Select(c => new
                {
                    c.CourseId,
                    c.Courses.CourseName,
                    c.Courses.CourseImage
                })
            });

            if (result == null || !result.Any())
                return NotFound(new { message = "No tracks found." });

            return Ok(result);
        }

        // get track details
        [HttpGet("track-details/{trackId}")]
        public async Task<IActionResult> GetTrackDetails(int trackId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null)
                return Unauthorized();

            var track = await _context.CourseTracks
                .Include(t => t.CourseTrackCourses)!
                .ThenInclude(ctc => ctc.Courses)
                .FirstOrDefaultAsync(t => t.TrackId == trackId && t.CourseTrackCourses!.Any(c => c.Courses.InstructorId == instructorId));

            if (track == null)
                return NotFound(new { message = "Track not found or not yours." });
            var result = new
            {
                track.TrackId,
                track.TrackName,
                track.TrackDescription,
                track.CreatedAt,
                Courses = track.CourseTrackCourses!.Select(c => new
                {
                    c.CourseId,
                    c.Courses.CourseName,
                    c.Courses.CourseImage
                })
            };
            if (result == null || !result.Courses.Any())
                return NotFound(new { message = "No courses found in this track." });

            return Ok(result);
        }

        #endregion

        #region Course

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

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data.", ModelState });
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

            TrackInstructorActions("Create", $"Course created successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
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

            TrackInstructorActions("Update", $"Course updated successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
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

            TrackInstructorActions("Delete", $"Course soft-deleted successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
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

            TrackInstructorActions("Toggle", $"Course status toggled successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");

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

        #region Level

        // create level
        [HttpPost("create-level")]
        public async Task<IActionResult> CreateLevel([FromBody] CreateLevelInput input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == input.CourseId && c.InstructorId == instructorId);
            if (course == null)
                return NotFound(new { message = "Course not found or not owned by you." });

            int nextOrder = await _context.Levels.Where(l => l.CourseId == input.CourseId).CountAsync() + 1;

            // if it's the first level, no need to require previous level completion
            bool requiresPrevious = nextOrder == 1 ? false : true;

            var level = new Level
            {
                CourseId = input.CourseId,
                LevelOrder = nextOrder,
                LevelName = input.LevelName.Trim(),
                LevelDetails = input.LevelDetails?.Trim()!,
                IsVisible = input.IsVisible,
                RequiresPreviousLevelCompletion = requiresPrevious
            };


            _context.Levels.Add(level);
            await _context.SaveChangesAsync();

            TrackInstructorActions("Create", $"Level created successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Level created successfully!", level });
        }

        // update level
        [HttpPut("update-level")]
        public async Task<IActionResult> UpdateLevel([FromBody] UpdateLevelInput input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var level = await _context.Levels
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LevelId == input.LevelId && l.Course.InstructorId == instructorId);

            if (level == null)
                return NotFound(new { message = "Level not found or not yours." });

            if (!string.IsNullOrWhiteSpace(input.LevelName))
                level.LevelName = input.LevelName.Trim();

            if (!string.IsNullOrWhiteSpace(input.LevelDetails))
                level.LevelDetails = input.LevelDetails.Trim();

            await _context.SaveChangesAsync();
            TrackInstructorActions("Update", $"Level updated successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Level updated successfully!", level });
        }

        // delete level
        [HttpDelete("delete-level/{levelId}")]
        public async Task<IActionResult> DeleteLevel(int levelId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var level = await _context.Levels
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LevelId == levelId && l.Course.InstructorId == instructorId);

            if (level == null)
                return NotFound(new { message = "Level not found or not yours." });


            level.IsDeleted = true;
            await _context.SaveChangesAsync();

            TrackInstructorActions("Delete", $"Level deleted successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Level deleted successfully." });
        }

        // get all levels for this course
        [HttpGet("course-levels/{courseId}")]
        public async Task<IActionResult> GetCourseLevels(int courseId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
            if (course == null)
                return NotFound(new { message = "Course not found or not yours." });

            var levels = await _context.Levels
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.LevelOrder)
                .ToListAsync();

            return Ok(new { count = levels.Count, levels });
        }

        // show/ hide level
        [HttpPost("toggle-level-visibility/{levelId}")]
        public async Task<IActionResult> ToggleLevelVisibility(int levelId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var level = await _context.Levels
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LevelId == levelId && l.Course.InstructorId == instructorId);

            if (level == null)
                return NotFound(new { message = "Level not found or not yours." });

            level.IsVisible = !level.IsVisible;
            await _context.SaveChangesAsync();

            TrackInstructorActions("Toggle", $"Level visibility toggled successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new
            {
                message = level.IsVisible ? "Level is now visible." : "Level is now hidden.",
                status = level.IsVisible ? "visible" : "hidden"
            });
        }

        // reorder levels
        [HttpPost("reorder-course-levels")]
        public async Task<IActionResult> ReorderLevels([FromBody] List<ReorderLevelInput> input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            foreach (var item in input)
            {
                var level = await _context.Levels
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.LevelId == item.LevelId && l.Course.InstructorId == instructorId);

                if (level != null)
                {
                    level.LevelOrder = item.NewOrder;
                }
            }

            await _context.SaveChangesAsync();
            TrackInstructorActions("Reorder", $"Levels reordered successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Levels reordered successfully." });
        }

        // level status 
        [HttpGet("level-stats/{levelId}")]
        public async Task<IActionResult> GetLevelStats(int levelId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var level = await _context.Levels
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LevelId == levelId && l.Course.InstructorId == instructorId);

            if (level == null)
                return NotFound(new { message = "Level not found or not yours." });

            var usersReached = await _context.UserProgresses
                .CountAsync(p => p.CurrentLevelId == levelId);

            return Ok(new
            {
                level.LevelId,
                level.LevelName,
                usersReached
            });
        }

        #endregion

        #region Section

        // create section
        [HttpPost("create-section")]
        public async Task<IActionResult> CreateSection([FromBody] CreateSectionInput input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var level = await _context.Levels
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LevelId == input.LevelId && l.Course.InstructorId == instructorId);

            if (level == null)
                return NotFound(new { message = "Level not found or not yours." });

            int nextOrder = await _context.Sections
                .Where(s => s.LevelId == input.LevelId)
                .CountAsync() + 1;

            // if it's the first section, no need to require previous section completion
            bool requiresPrevious = nextOrder == 1 ? false : true;

            var section = new Section
            {
                LevelId = input.LevelId,
                SectionName = input.SectionName.Trim(),
                SectionOrder = nextOrder,

                RequiresPreviousSectionCompletion = requiresPrevious

            };

            _context.Sections.Add(section);
            await _context.SaveChangesAsync();
            TrackInstructorActions("Create", $"Section created successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Section created successfully!", section });
        }

        // update section
        [HttpPut("update-section")]
        public async Task<IActionResult> UpdateSection([FromBody] UpdateSectionInput input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var section = await _context.Sections
                .Include(s => s.Level)
                .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(s => s.SectionId == input.SectionId && s.Level.Course.InstructorId == instructorId);

            if (section == null)
                return NotFound(new { message = "Section not found or not yours." });

            if (!string.IsNullOrWhiteSpace(input.SectionName))
                section.SectionName = input.SectionName.Trim();

            await _context.SaveChangesAsync();
            TrackInstructorActions("Update", $"Section updated successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Section updated successfully!", section });
        }

        // delete section
        [HttpDelete("delete-section/{sectionId}")]
        public async Task<IActionResult> DeleteSection(int sectionId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var section = await _context.Sections
                .Include(s => s.Level)
                .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(s => s.SectionId == sectionId && s.Level.Course.InstructorId == instructorId);

            if (section == null)
                return NotFound(new { message = "Section not found or not yours." });

            section.IsDeleted = true;
            await _context.SaveChangesAsync();

            TrackInstructorActions("Delete", $"Section deleted successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Section deleted successfully." });
        }

        // reorder sections
        [HttpPost("reorder-sections")]
        public async Task<IActionResult> ReorderSections([FromBody] List<ReorderSectionInput> input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            foreach (var item in input)
            {
                var section = await _context.Sections
                    .Include(s => s.Level)
                    .ThenInclude(l => l.Course)
                    .FirstOrDefaultAsync(s => s.SectionId == item.SectionId && s.Level.Course.InstructorId == instructorId);

                if (section != null)
                {
                    section.SectionOrder = item.NewOrder;
                }
            }

            await _context.SaveChangesAsync();
            TrackInstructorActions("Reorder", $"Sections reordered successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Sections reordered successfully." });
        }

        // toggle section visibility
        [HttpPost("toggle-section-visibility/{sectionId}")]
        public async Task<IActionResult> ToggleSectionVisibility(int sectionId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var section = await _context.Sections
                .Include(s => s.Level)
                .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(s => s.SectionId == sectionId && s.Level.Course.InstructorId == instructorId);

            if (section == null)
                return NotFound(new { message = "Section not found or not yours." });

            section.IsVisible = !section.IsVisible;
            await _context.SaveChangesAsync();


            TrackInstructorActions("Toggle", $"Section visibility toggled successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new
            {
                message = section.IsVisible ? "Section is now visible." : "Section is now hidden.",
                status = section.IsVisible ? "visible" : "hidden"
            });
        }

        // section status 
        [HttpGet("section-stats/{sectionId}")]
        public async Task<IActionResult> GetSectionStats(int sectionId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var section = await _context.Sections
                .Include(s => s.Level)
                .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(s => s.SectionId == sectionId && s.Level.Course.InstructorId == instructorId);

            if (section == null)
                return NotFound(new { message = "Section not found or not yours." });

            var usersReached = await _context.UserProgresses
                .CountAsync(p => p.CurrentSectionId == sectionId);

            return Ok(new
            {
                section.SectionId,
                section.SectionName,
                usersReached
            });
        }
        #endregion

        #region Content

        // create content
        [HttpPost("create-content")]
        public async Task<IActionResult> CreateContent([FromBody] CreateContentInput input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var section = await _context.Sections
                .Include(s => s.Level)
                .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(s => s.SectionId == input.SectionId && s.Level.Course.InstructorId == instructorId);

            if (section == null)
                return NotFound(new { message = "Section not found or not yours." });

            if (input.ContentType == DTO.Courses.ContentType.Video && string.IsNullOrWhiteSpace(input.ContentUrl))
                return BadRequest(new { message = "Video URL is required for video content." });

            if (input.ContentType == DTO.Courses.ContentType.Doc && string.IsNullOrWhiteSpace(input.ContentDoc))
                return BadRequest(new { message = "Document path is required for doc content." });

            if (input.ContentType == DTO.Courses.ContentType.Text && string.IsNullOrWhiteSpace(input.ContentText))
                return BadRequest(new { message = "Text is required for text content." });

            int nextOrder = await _context.Contents
                .Where(c => c.SectionId == input.SectionId)
                .CountAsync() + 1;

            var content = new Content
            {
                SectionId = input.SectionId,
                Title = input.Title.Trim(),
                ContentType = (Models.Courses.ContentType)input.ContentType,
                ContentText = input.ContentType == DTO.Courses.ContentType.Text ? input.ContentText : null,
                ContentUrl = input.ContentType == DTO.Courses.ContentType.Video ? input.ContentUrl : null,
                ContentDoc = input.ContentType == DTO.Courses.ContentType.Doc ? input.ContentDoc : null,
                DurationInMinutes = input.DurationInMinutes,
                ContentDescription = input.ContentDescription,
                ContentOrder = nextOrder
            };


            _context.Contents.Add(content);
            await _context.SaveChangesAsync();
            TrackInstructorActions("Create", $"Content created successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Content created successfully.", content });
        }

        [HttpPost("upload-content-file")]
        public async Task<IActionResult> UploadContentFile(IFormFile file, [FromQuery] DTO.Courses.ContentType type)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var folderName = type == DTO.Courses.ContentType.Video ? "videos" : "docs";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/{folderName}/{fileName}";
            return Ok(new { message = "File uploaded successfully.", url });
        }

        // update content
        [HttpPut("update-content")]
        public async Task<IActionResult> UpdateContent([FromBody] UpdateContentInput input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var content = await _context.Contents
                .Include(c => c.Section)
                .ThenInclude(s => s.Level)
                .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(c => c.ContentId == input.ContentId && c.Section.Level.Course.InstructorId == instructorId);

            if (content == null)
                return NotFound(new { message = "Content not found or not yours." });

            if (!string.IsNullOrWhiteSpace(input.Title))
                content.Title = input.Title.Trim();

            if (!string.IsNullOrWhiteSpace(input.ContentDescription))
                content.ContentDescription = input.ContentDescription.Trim();

            if (input.DurationInMinutes.HasValue)
                content.DurationInMinutes = input.DurationInMinutes.Value;


            if (content.ContentType == Models.Courses.ContentType.Text && !string.IsNullOrWhiteSpace(input.ContentText))
            {
                content.ContentText = input.ContentText;
            }

            if (content.ContentType == Models.Courses.ContentType.Video && !string.IsNullOrWhiteSpace(input.ContentUrl))
            {
                content.ContentUrl = input.ContentUrl;
            }

            if (content.ContentType == Models.Courses.ContentType.Doc && !string.IsNullOrWhiteSpace(input.ContentDoc))
            {
                content.ContentDoc = input.ContentDoc;
            }

            await _context.SaveChangesAsync();
            TrackInstructorActions("Update", $"Content updated successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Content updated successfully.", content });
        }


        // delete content
        [HttpDelete("delete-content/{contentId}")]
        public async Task<IActionResult> DeleteContent(int contentId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var content = await _context.Contents
                .Include(c => c.Section)
                .ThenInclude(s => s.Level)
                .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(c => c.ContentId == contentId && c.Section.Level.Course.InstructorId == instructorId);

            if (content == null)
                return NotFound(new { message = "Content not found or not yours." });

            _context.Contents.Remove(content);
            await _context.SaveChangesAsync();

            TrackInstructorActions("Delete", $"Content deleted successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Content deleted successfully." });
        }

        // reorder contents
        [HttpPost("reorder-contents")]
        public async Task<IActionResult> ReorderContents([FromBody] List<ReorderContentInput> input)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            foreach (var item in input)
            {
                var content = await _context.Contents
                    .Include(c => c.Section)
                    .ThenInclude(s => s.Level)
                    .ThenInclude(l => l.Course)
                    .FirstOrDefaultAsync(c => c.ContentId == item.ContentId && c.Section.Level.Course.InstructorId == instructorId);

                if (content != null)
                {
                    content.ContentOrder = item.NewOrder;
                }
            }

            await _context.SaveChangesAsync();
            TrackInstructorActions("Reorder", $"Contents reordered successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new { message = "Content reordered successfully." });
        }

        // toggle content visibility
        [HttpPost("toggle-content-visibility/{contentId}")]
        public async Task<IActionResult> ToggleContentVisibility(int contentId)
        {
            var instructorId = GetUserIdFromToken();
            if (instructorId == null) return Unauthorized();

            var content = await _context.Contents
                .Include(c => c.Section)
                .ThenInclude(s => s.Level)
                .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(c => c.ContentId == contentId && c.Section.Level.Course.InstructorId == instructorId);

            if (content == null)
                return NotFound(new { message = "Content not found or not yours." });

            content.IsVisible = !content.IsVisible;
            await _context.SaveChangesAsync();

            TrackInstructorActions("Toggle", $"Content visibility toggled successfully by {GetUserNameFromToken()} his role is {GetUserRoleFromToken()}");
            return Ok(new
            {
                message = content.IsVisible ? "Content is now visible." : "Content is now hidden.",
                status = content.IsVisible ? "visible" : "hidden"
            });
        }


        #endregion

        #region DashBoard
        [HttpGet("dashboard-course-stats")]
        public async Task<IActionResult> GetCourseStates()
        {
            var userId = GetUserIdFromToken();
            var role = GetUserRoleFromToken();

            if(userId == null)
                return Unauthorized(new { message = "Invalid or missing token." });

            if (role == null) 
                return Forbid("You are not authorized to view this data.");

            var course = await _context.Courses
                .Where(c => !c.IsDeleted && c.InstructorId == userId)
                .ToListAsync();

            var courseStats = await Task.WhenAll(course.Select(async course =>
            {
                var studentCount = await _context.CourseEnrollments.CountAsync(e => e.CourseId == course.CourseId);
                var progressCount = await _context.UserProgresses.CountAsync(p => p.CourseId == course.CourseId);

                return new
                {
                    course.CourseId,
                    course.CourseName,
                    course.CourseImage,
                    StudentCount = studentCount,
                    ProgressCount = progressCount
                };
            }));

            var mostEngagedCourse = courseStats.OrderByDescending(c => c.ProgressCount).FirstOrDefault();

            return Ok(new
            {
                TotalCourses = courseStats.Length,
                Courses = courseStats,
                MostEngagedCourse = mostEngagedCourse != null
                    ? new
                    {
                        mostEngagedCourse.CourseId,
                        mostEngagedCourse.CourseName,
                        mostEngagedCourse.ProgressCount
                    }
                    : null
            });
        }

        #endregion
        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return null;

            return userId;
        }

        private string? GetUserNameFromToken()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value;
        }

        private string? GetUserRoleFromToken()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        private void TrackInstructorActions(string actionType, string description)
        {
            var userId = GetUserIdFromToken();

            if (userId == null)
                return;

            var log = new InstructorActionLog
            {
                InstructorId = userId.Value,

                ActionType = actionType,
                ActionDescription = description,
                ActionDate = DateTime.UtcNow
            };

            _context.InstructorActionLogs.Add(log);
            _context.SaveChanges();
        }

        // Format the skill text
        private static string FormatText(string skill)
        {
            if (string.IsNullOrWhiteSpace(skill))
                return string.Empty;

            // Remove extra spaces
            skill = skill.Trim();       // Trim spaces at ends
            skill = System.Text.RegularExpressions.Regex.Replace(skill, @"\s+", " "); // Remove inner double spaces

            // Capitalize first letter
            if (skill.Length == 1)
                return skill.ToUpper();

            return char.ToUpper(skill[0]) + skill.Substring(1).ToLower();
        }


    }
}
