using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GraduationProjectBackendAPI.Models.User;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public enum PaymentStatus
    {
        Pending = 0, Completed = 1, Failed = 2
    }

    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentId { get; set; } // Primary Key

        [ForeignKey("User")]
        public int UserId { get; set; } // Foreign Key إلى المستخدم

        [ForeignKey("Courses")]
        public int CourseId { get; set; } // Foreign Key إلى الكورس

        public decimal Amount { get; set; } // المبلغ المدفوع
        public DateTime PaymentDate { get; set; } // تاريخ الدفع
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending; // حالة الدفع
        public string TransactionId { get; set; } // رقم المعاملة المالية

        // العلاقات
        public virtual Users User { get; set; }
        public virtual Courses Course { get; set; }
    }
}
