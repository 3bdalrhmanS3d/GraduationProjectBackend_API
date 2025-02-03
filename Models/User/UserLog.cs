using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Models.User
{
    public class UserLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogId { get; set; } // Primary Key

        [ForeignKey("User")]
        public int UserId { get; set; } // Foreign Key للمستخدم

        public string Action { get; set; } // وصف النشاط
        public string IPAddress { get; set; } // عنوان الـ IP الخاص بالمستخدم
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // وقت النشاط

        // العلاقات
        public virtual Users User { get; set; }
    }
}
