using GraduationProjectBackendAPI.Models.Courses;
using GraduationProjectBackendAPI.Models.User;
using Microsoft.EntityFrameworkCore;

namespace GraduationProjectBackendAPI.Models.AppDBContext
{
    public class AppDbContext : DbContext
    {
        // Add-Migration init0 -Context AppDbContext

        // اخر ابديت حل كان رقم 
        // 0  abdo saad  

        // note
        // لما تعمل المايجريشن وقبل ما تعمل ابديت 
        // اي 
        // onDelete: ReferentialAction.Cascade);
        // حولها الى
        // onDelete: ReferentialAction.NoAction);


        // Update-Database -Context AppDbContext

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // User folder 
        public DbSet<User.Users> UsersT { get; set; }
        public DbSet<UserDetails> DetailsT { get; set; }
        public DbSet<UserVisitHistory> UserVisitHistoryT { get; set; }
        public DbSet<AccountVerification> AccountVerificationT { get; set; }
        public DbSet<BlacklistToken> BlacklistTokensT { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<AIChat> AIChatT { get; set; }

        public DbSet<AdminActionLog> adminActionLogs { get; set; }

        public DbSet<Notification> NotificationT { get; set; }
        public DbSet<InstructorActionLog> InstructorActionLogs { get; set; }
        // Courses Platform Tables
        public DbSet<Courses.Courses> Courses { get; set; }
        public DbSet<Level> Levels { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Content> Contents { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<TaskT> TaskTs { get; set; }
        public DbSet<UserProgress> UserProgresses { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<CourseReview> CourseReviews { get; set; }
        public DbSet<AboutCourse> aboutCourses { get; set; }
        public DbSet<CourseSkill> courseSkills { get; set; }
        public DbSet<UserTask> UserTasks { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<CourseFeedback> CourseFeedbacks { get; set; }

        public DbSet<FavoriteCourse> FavoriteCourses { get; set; }
        public DbSet<CourseTrack> CourseTracks { get; set; }
        public DbSet<CourseTrackCourse> CourseTrackCourses { get; set; }

        // DbSets for the existing models

        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<CorrectAnswer> CorrectAnswers { get; set; }
        public DbSet<UserAnswer> UserAnswers { get; set; }
        public DbSet<UserCoursePoints> UserCoursePoints { get; set; }

    }
}
