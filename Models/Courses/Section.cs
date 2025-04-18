using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class Section
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SectionId { get; set; } // Primary Key

        [ForeignKey("Level")]
        public int LevelId { get; set; } // Foreign Key
        public string SectionName { get; set; }
        public int SectionOrder { get; set; }

        public bool IsDeleted { get; set; } = false; // Soft Delete Flag
        public bool IsVisible { get; set; } = true;
        public bool RequiresPreviousSectionCompletion { get; set; } = false;

        // Navigation Properties
        public virtual Level? Level { get; set; }
        public ICollection<Content>? Contents { get; set; }
        public virtual Quiz? Quiz { get; set; }
        public virtual TaskT? Task { get; set; }
    }
}
