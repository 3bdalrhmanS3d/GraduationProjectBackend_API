using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GraduationProjectBackendAPI.Models.User;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class CourseEnrollment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CourseEnrollmentId { get; set; } // Primary Key
        [ForeignKey("User")]
        public int UserId { get; set; } // Foreign Key
        [ForeignKey("Course")]
        public int CourseId { get; set; } // Foreign Key
        public DateTime EnrolledAt { get; set; }

        // Navigation Properties
        public virtual Users User { get; set; }
        public virtual Courses Course { get; set; }
    }
}
