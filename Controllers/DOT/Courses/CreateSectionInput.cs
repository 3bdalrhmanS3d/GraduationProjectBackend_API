using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Controllers.DOT.Courses
{
    public class CreateSectionInput
    {
        [Required]
        public int LevelId { get; set; }

        [Required]
        public string SectionName { get; set; }

        

    }
}
