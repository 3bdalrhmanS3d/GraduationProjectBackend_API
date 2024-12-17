using GraduationProjectBackendAPI.Models.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class CourseReview
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CourseReviewId { get; set; } // Primary Key
        [ForeignKey("User")]
        public int UserId { get; set; } // Foreign Key
        [ForeignKey("Course")]
        public int CourseId { get; set; } // Foreign Key
        public int Rating { get; set; } // تقييم من 1 إلى 5
        public string ReviewComment { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual Users User { get; set; }
        public virtual Courses Course { get; set; }
    }
}
