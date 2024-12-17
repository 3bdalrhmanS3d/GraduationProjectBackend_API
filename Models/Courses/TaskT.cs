using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GraduationProjectBackendAPI.Models.User;
namespace GraduationProjectBackendAPI.Models.Courses
{
    public class TaskT
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TaskId { get; set; } // Primary Key

        [ForeignKey("Section")]
        public int SectionId { get; set; } // Foreign Key
        public string TaskDescription { get; set; }
        public string UploadFileUrl { get; set; }
        public string Status { get; set; } // Pending, Passed, Failed

        // Navigation Properties
        public virtual Section Section { get; set; }
    }
}
