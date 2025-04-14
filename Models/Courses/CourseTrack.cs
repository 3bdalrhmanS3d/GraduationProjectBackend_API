using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class CourseTrack
    {
        [Key]
        public int TrackId { get; set; }

        [Required]
        public string TrackName { get; set; }

        public string? TrackDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<CourseTrackCourse> CourseTrackCourses { get; set; }
    }
}
