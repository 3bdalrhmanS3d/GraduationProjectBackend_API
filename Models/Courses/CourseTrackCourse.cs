using GraduationProjectBackendAPI.Models.Courses;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CourseTrackCourse
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("CourseTrack")]
    public int TrackId { get; set; }

    [ForeignKey("Courses")]
    public int CourseId { get; set; }

    public virtual CourseTrack CourseTrack { get; set; }
    public virtual Courses Courses { get; set; }
}
