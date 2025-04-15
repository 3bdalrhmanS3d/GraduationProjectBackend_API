using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.DOT.Courses
{
    public class UpdateLevelInput
    {
        [Required]
        public int LevelId { get; set; }

        public string? LevelName { get; set; }
        public string? LevelDetails { get; set; }
        public bool? IsVisible { get; set; }
        public bool? RequiresPreviousLevelCompletion { get; set; } = false;
    }
}
