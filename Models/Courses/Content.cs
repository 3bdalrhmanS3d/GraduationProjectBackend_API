using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public enum ContentType
    {
        Text = 0,
        Video = 1,
        Doc = 2,

    }
    public class Content
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ContentId { get; set; } // Primary Key

        [ForeignKey("Section")]
        public int SectionId { get; set; } // Foreign Key
        public ContentType ContentType { get; set; } = ContentType.Text;
        public string? ContentUrl { get; set; } // if video 
        public string? ContentText { get; set; } // if text 
        public string? ContentDoc { get; set; } // if doc 
        public int DurationInMinutes { get; set; }

        public virtual Section Section { get; set; }
    }
}
