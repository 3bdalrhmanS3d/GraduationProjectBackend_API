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

        // Navigation Properties
        public virtual Section Section { get; set; }
    }
}
