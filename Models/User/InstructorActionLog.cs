using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Models.User
{
    public class InstructorActionLog
    {
        [Key]
        public int LogId { get; set; }

        [ForeignKey("User")]
        public int InstructorId { get; set; }

        [Required]
        public string ActionType { get; set; } = string.Empty;  // مثال: CreateCourse, UpdateLevel

        [Required]
        public string ActionDescription { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Users User { get; set; }
    }
}
