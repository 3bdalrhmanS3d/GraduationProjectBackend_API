using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.DOT.Courses
{
    public class CreateCourseInput
    {
        [Required]
        public string CourseName { get; set; }

        [Required]
        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CoursePrice { get; set; }

        public string? CourseImage { get; set; }

        public bool IsActive { get; set; }

        public List<AboutCourseInput> AboutCourseTexts { get; set; } = new();
        public List<CourseSkillInput> CourseSkills { get; set; } = new();
    }
}
