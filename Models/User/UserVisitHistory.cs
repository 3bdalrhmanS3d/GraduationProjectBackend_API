using GraduationProjectBackendAPI.Models.User;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models
{
    public class UserVisitHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int HisId { get; set; } 

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; } // FK referencing User.UserId

        [Required]
        public DateTime LastVisit { get; set; } 
        public virtual Users User { get; set; }
    }
}
