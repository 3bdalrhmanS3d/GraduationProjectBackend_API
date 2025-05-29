using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.DTO.Courses
{
    public class UpdateSectionInput
    {
        [Required]
        public int SectionId { get; set; }

        public string? SectionName { get; set; }
    }
}
