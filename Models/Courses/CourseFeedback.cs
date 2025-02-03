using GraduationProjectBackendAPI.Models.User;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class CourseFeedback
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FeedbackId { get; set; } // Primary Key

        [ForeignKey("User")]
        public int UserId { get; set; } // Foreign Key للمستخدم

        [ForeignKey("Course")]
        public int CourseId { get; set; } // Foreign Key للكورس


        public int Rating { get; set; } // التقييم (من 1 إلى 5)
        public string Comment { get; set; } // تعليق المستخدم
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // وقت التعليق

        // العلاقات
        public virtual Users User { get; set; }
        public virtual Courses Course { get; set; }
    }
}
