using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.DOT.Courses
{
    public class CreateLevelInput
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public string LevelName { get; set; }

        public string? LevelDetails { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool RequiresPreviousLevelCompletion { get; set; } = false;
    }
}
