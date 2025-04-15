using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.DOT.Courses
{
    public class UpdateSectionInput
    {
        [Required]
        public int SectionId { get; set; }

        public string? SectionName { get; set; }
    }
}
