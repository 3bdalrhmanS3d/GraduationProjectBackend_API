using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.DOT.Courses
{
    public class CreateTrackInput
    {
        [Required]
        public string TrackName { get; set; }

        public string? TrackDescription { get; set; }

        public List<int>? CourseIds { get; set; }
    }
}
