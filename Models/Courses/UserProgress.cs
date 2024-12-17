using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GraduationProjectBackendAPI.Models.User;
namespace GraduationProjectBackendAPI.Models.Courses
{
    public class UserProgress
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserProgressId { get; set; } // Primary Key
        [ForeignKey("User")]
        public int UserId { get; set; } // Foreign Key
        [ForeignKey("Course")]
        public int CourseId { get; set; } // Foreign Key

        public int CurrentLevelId { get; set; } // مستوى المستخدم الحالي
        public int CurrentSectionId { get; set; } // السكشن الحالي
        public DateTime LastUpdated { get; set; }

        // Navigation Properties
        public virtual Users User { get; set; }
        public virtual Courses Course { get; set; }
    }
}
