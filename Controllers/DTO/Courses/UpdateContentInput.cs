using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.DTO.Courses
{
    public class UpdateContentInput
    {
        [Required]
        public int ContentId { get; set; }

        public string? Title { get; set; }
        public string? ContentText { get; set; }
        public string? ContentUrl { get; set; }
        public string? ContentDoc { get; set; }
        public string? ContentDescription { get; set; }
        public int? DurationInMinutes { get; set; }
        public bool? IsVisible { get; set; }
    }
}
