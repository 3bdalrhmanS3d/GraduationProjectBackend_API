using GraduationProjectBackendAPI.Models.Courses;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.User
{
    public class Users
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        // Password complexity rules should be implemented (e.g., minimum length, inclusion of special characters)
        [Required]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]).{8,}$", ErrorMessage = "Password must be at least 8 characters long, and contain at least one letter, one number, and one special character.")]
        public string PasswordHash { get; set; }

        // Timestamp indicating when the user account was created.
        public DateTime CreatedAt { get; set; }

        public UserDetails UserDetails { get; set; }
        public ICollection<UserVisitHistory>? UserVisitHistories { get; set; }
        public AccountVerification AccountVerification { get; set; }
        public ICollection<UserProgress> UserProgresses { get; set; }
        public ICollection<CourseEnrollment> CourseEnrollments { get; set; }
        public ICollection<CourseReview> CourseReviews { get; set; }

    }
}
