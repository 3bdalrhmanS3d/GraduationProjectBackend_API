using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public enum Outcame
    {
        learn = 0,
        expertise = 1,
    }
    public class AboutCourse
    {
        [Key]
        public int AboutCourseId { get; set; }

        [ForeignKey("Courses")]
        public int CourseId { get; set; }
        public string AboutCourseText { get; set; } = string.Empty;

        public Outcame outcametype = Outcame.learn; 
        public virtual Courses ? Courses { get; set; }

    }
}
