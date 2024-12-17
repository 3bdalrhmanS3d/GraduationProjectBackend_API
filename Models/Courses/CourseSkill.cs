using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class CourseSkill
    {
        [Key]
        public int CourseSkillId { get; set; }

        [ForeignKey("Courses")]
        public int CourseId { get; set; }
        public string CourseSkillText { get; set; } = string.Empty;

        public virtual Courses? Courses { get; set; }
    }
}
