using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class Courses
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CourseId { get; set; } // Primary Key
        public string CourseName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Level>? Levels { get; set; }
        public ICollection<UserProgress>? UserProgresses { get; set; }
        public ICollection<CourseEnrollment>? CourseEnrollments { get; set; }
        public ICollection<CourseReview>? CourseReviews { get; set; }

    }
}
