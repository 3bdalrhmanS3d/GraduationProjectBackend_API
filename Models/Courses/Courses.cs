using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GraduationProjectBackendAPI.Models.User;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class Courses
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CourseId { get; set; } // Primary Key
        public string CourseName { get; set; }


        // هتسجله بالايميل وهو هيجيب ال id 
        [ForeignKey("User")]
        public int InstructorId { get; set; }

        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual Users User { get; set; }

        public ICollection<AboutCourse>? aboutCourses { get; set; }
        public ICollection<CourseSkill>? CourseSkills { get; set; }
        public ICollection<Level>? Levels { get; set; }
        public ICollection<UserProgress>? UserProgresses { get; set; }
        public ICollection<CourseEnrollment>? CourseEnrollments { get; set; }
        public ICollection<CourseReview>? CourseReviews { get; set; }
        public ICollection<UserCoursePoints> userCoursePoints { get; set; }

    }
}
