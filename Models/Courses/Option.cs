using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class Option
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OptionId { get; set; } // Primary Key

        [ForeignKey("Question")]
        public int QuestionId { get; set; } // Foreign Key to Question

        public string OptionText { get; set; }
        public bool IsCorrect { get; set; } // هل هذه الإجابة صحيحة أم لا؟

        // Navigation Properties
        public virtual Question Question { get; set; }
    }
}
