using GraduationProjectBackendAPI.Models.Courses;

namespace GraduationProjectBackendAPI.DTO.Courses
{
    public class AboutCourseInput
    {
        public int AboutCourseId { get; set; } // 0 for new
        public string AboutCourseText { get; set; }
        public Outcame OutcameType { get; set; } = Outcame.learn;
    }
}
