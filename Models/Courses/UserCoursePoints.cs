using GraduationProjectBackendAPI.Models.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class UserCoursePoints
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserCoursePointsId { get; set; } // Primary Key

        [ForeignKey("User")]
        public int UserId { get; set; } // Foreign Key to Users

        [ForeignKey("Course")]
        public int CourseId { get; set; } // Foreign Key to Course

        public int TotalPoints { get; set; }

        // Navigation Properties
        public virtual Users User { get; set; }
        public virtual Courses Course { get; set; }
    }
}
