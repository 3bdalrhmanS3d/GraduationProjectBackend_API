using GraduationProjectBackendAPI.Models.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class UserAnswer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserAnswerId { get; set; } // Primary Key

        [ForeignKey("User")]
        public int UserId { get; set; } // Foreign Key to Users

        [ForeignKey("Quiz")]
        public int QuizId { get; set; } // Foreign Key to Quiz

        [ForeignKey("Question")]
        public int QuestionId { get; set; } // Foreign Key to Question

        public string UserAnswerText { get; set; } // الإجابة التي قدمها المستخدم، النص في حالة الأسئلة المكتوبة
        public bool IsCorrect { get; set; } // هل الإجابة صحيحة؟
        public int Score { get; set; } // النقاط التي حصل عليها المستخدم للإجابة الصحيحة

        // Navigation Properties
        public virtual Users User { get; set; }
        public virtual Quiz Quiz { get; set; }
        public virtual Question Question { get; set; }
    }
}
