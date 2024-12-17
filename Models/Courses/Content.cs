using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class Content
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ContentId { get; set; } // Primary Key

        [ForeignKey("Section")]
        public int SectionId { get; set; } // Foreign Key
        public string ContentType { get; set; } 
        public string ContentUrl { get; set; } 
        public string ContentText { get; set; } 
        public int DurationInMinutes { get; set; }

        public virtual Section Section { get; set; }
    }
}
