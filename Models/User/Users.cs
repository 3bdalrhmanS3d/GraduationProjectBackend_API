using GraduationProjectBackendAPI.Models.Courses;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.User
{
    public enum UserRole
    {
        [Description("RegularUser")]
        RegularUser,
        [Description("Instructor")]
        Instructor,
        [Description("Admin")]
        Admin
    }

    public class Users
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        // Password complexity rules should be implemented (e.g., minimum length, inclusion of special characters)
        [Required]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]).{8,}$", ErrorMessage = "Password must be at least 8 characters long, and contain at least one letter, one number, and one special character.")]
        public string PasswordHash { get; set; }

        // Timestamp indicating when the user account was created.
        public DateTime CreatedAt { get; set; }

        [Required]
        [DefaultValue(UserRole.RegularUser)]

        public string? ProfilePhoto { get; set; }
        public UserRole Role { get; set; } = UserRole.RegularUser;

        public UserDetails UserDetails { get; set; }
        public ICollection<UserVisitHistory>? UserVisitHistories { get; set; }
        public AccountVerification AccountVerification { get; set; }
        public ICollection<UserProgress> UserProgresses { get; set; }
        public ICollection<CourseEnrollment> CourseEnrollments { get; set; }
        public ICollection<CourseReview> CourseReviews { get; set; }
        public ICollection<UserAnswer> userAnswers { get; set; }
        public ICollection<UserCoursePoints> userCoursePoints { get; set; }
        
        public virtual ICollection<Payment>? Payments { get; set; } 
        public virtual ICollection<UserLog>? UserLogs { get; set; } 
        public virtual ICollection<AIChat>? AIChats { get; set; }
        public virtual ICollection<CourseFeedback>? Feedbacks { get; set; }
        public ICollection<FavoriteCourse>? FavoriteCourses { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }

        public virtual ICollection<InstructorActionLog>? InstructorActions { get; set; }
    }
}
