using GraduationProjectBackendAPI.Models.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models
{
    public class UserDetails
    {
        [Key]
        [ForeignKey("User")]
        public int UserId { get; set; } 

        [Required]
        public DateTime BirthDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Edu { get; set; } // 'Primary', 'Middle', 'High School', 'University'

        [Required]
        [StringLength(100)]
        public string National { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Users User { get; set; }
    }
}
