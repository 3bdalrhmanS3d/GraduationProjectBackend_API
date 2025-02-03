using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GraduationProjectBackendAPI.Models.User;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public enum UserTaskStatus
    {
        Pending = 0, Passed = 1 , Failed = 2
    }
    public class UserTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserTaskId { get; set; } // Primary Key

        [ForeignKey("User")]
        public int UserId { get; set; } // Foreign Key to Users

        [ForeignKey("TaskT")]
        public int TaskId { get; set; } // Foreign Key to TaskT

        public UserTaskStatus Status { get; set; } = UserTaskStatus.Pending; // Pending, Passed, Failed
        public string ReviewNotes { get; set; } // ملاحظات للمراجعة أو الرفض

        public DateTime TaskSubmitAt { get; set; } // تاريخ تسليم الواجب
        // Navigation Properties
        public virtual Users User { get; set; }
        public virtual TaskT TaskT { get; set; }

    }
}
