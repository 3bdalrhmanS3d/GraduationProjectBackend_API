using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Collections.Specialized.BitVector32;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class Level
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LevelId { get; set; } // Primary Key

        [ForeignKey("Course")]
        public int CourseId { get; set; } // Foreign Key
        public int LevelOrder { get; set; }
        public string LevelName { get; set; }

        public string LevelDetails { get; set; }

        // Navigation Properties
        public virtual Courses Course { get; set; }
        public ICollection<Section>? Sections { get; set; }
    }
}
