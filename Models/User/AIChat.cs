using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Models.User
{
    public class AIChat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ChatId { get; set; } // Primary Key

        [ForeignKey("User")]
        public int UserId { get; set; } // Foreign Key للمستخدم

        public string UserQuestion { get; set; } // سؤال المستخدم
        public string AIResponse { get; set; } // إجابة الذكاء الاصطناعي
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // وقت المحادثة

        // العلاقات
        public virtual Users User { get; set; }
    }
}
