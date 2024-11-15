using GraduationProjectBackendAPI.Models.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models
{
    public class AccountVerification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; } 

        [Required]
        [StringLength(6)]
        public string Code { get; set; } 

        [Required]
        public bool CheckedOK { get; set; } 

        [Required]
        public DateTime Date { get; set; } 

        public virtual Users User { get; set; }
    }
}
