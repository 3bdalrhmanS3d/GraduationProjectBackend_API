using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class Quiz
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QuizId { get; set; } // Primary Key

        [ForeignKey("Section")]
        public int SectionId { get; set; } // Foreign Key
        public string QuizTitle { get; set; }

        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();

        // Navigation Properties
        public virtual Section Section { get; set; }
    }
}
