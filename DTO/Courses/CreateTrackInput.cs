using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.DTO.Courses
{
    public class CreateTrackInput
    {
        [Required]
        public string TrackName { get; set; }

        public string? TrackDescription { get; set; }

        public string? TrackImage { get; set; }

        public List<int>? CourseIds { get; set; }
    }
}
