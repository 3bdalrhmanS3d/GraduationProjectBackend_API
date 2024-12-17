using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public enum QuestionType
    {
        MCQ = 1,
        Checkbox = 2,
        Written = 3
    }
    public class Question
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QuestionId { get; set; } // Primary Key

        [ForeignKey("Quiz")]
        public int QuizId { get; set; } // Foreign Key to Quiz

        public string QuestionText { get; set; }
        public QuestionType QuestionType { get; set; } // نوع السؤال (اختياري، متعدد الخيارات، مكتوب)

        public int Score { get; set; } // النقاط التي حصل عليها المستخدم للإجابة الصحيحة 
        public virtual ICollection<Option> Options { get; set; } = new List<Option>();
        public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
        public virtual CorrectAnswer CorrectAnswer { get; set; }
        // Navigation Properties
        public virtual Quiz Quiz { get; set; }
    }
}
