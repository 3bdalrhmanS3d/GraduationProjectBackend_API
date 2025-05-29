using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.DTO.Courses
{
    public enum ContentType
    {
        Text = 0,
        Video = 1,
        Doc = 2,

    }
    public class CreateContentInput
    {
        [Required]
        public int SectionId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public ContentType ContentType { get; set; }

        public string? ContentUrl { get; set; }
        public string? ContentText { get; set; }
        public string? ContentDoc { get; set; }

        public int DurationInMinutes { get; set; } = 0;

        public string? ContentDescription { get; set; }
    }
}
