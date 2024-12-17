using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class CorrectAnswer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CorrectAnswerId { get; set; } // Primary Key

        [ForeignKey("Question")]
        public int QuestionId { get; set; } // Foreign Key to Question

        public string CorrectAnswerText { get; set; } // نص الإجابة الصحيحة في حالة الأسئلة المكتوبة.

        // Navigation Properties
        public virtual Question Question { get; set; }
    }
}
