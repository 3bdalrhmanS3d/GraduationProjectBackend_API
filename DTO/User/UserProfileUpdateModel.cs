using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.DTO.User
{
    public class UserProfileUpdateModel
    {
        [Required]
        public DateTime BirthDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Edu { get; set; } // 'Primary', 'Middle', 'High School', 'University'

        [Required]
        [StringLength(100)]
        public string National { get; set; }
    }
}
