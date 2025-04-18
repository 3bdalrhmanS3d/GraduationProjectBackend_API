using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GraduationProjectBackendAPI.Models.User;
namespace GraduationProjectBackendAPI.Models.Courses
{
    public class UserProgress
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserProgressId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("Course")]
        public int CourseId { get; set; }

        [ForeignKey("Level")]
        public int CurrentLevelId { get; set; } // رابط مباشر للـ Level

        [ForeignKey("Section")]
        public int CurrentSectionId { get; set; } // رابط مباشر للـ Section

        [ForeignKey("Content")]
        public int? CurrentContentId { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Users? User { get; set; }
        public virtual Courses? Course { get; set; }
        public virtual Level? CurrentLevel { get; set; }
        public virtual Section? CurrentSection { get; set; }
        public virtual Content? CurrentContent { get; set; }
    }

}
