using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Models.User
{
    public class AdminActionLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogId { get; set; }

        [Required]
        public int AdminId { get; set; } // ID الخاص بالأدمن الذي قام بالعملية

        [Required]
        public int TargetUserId { get; set; } // ID الخاص بالمستخدم الذي تم التعديل عليه

        [Required]
        public string ActionType { get; set; } // نوع العملية (MakeInstructor, MakeAdmin, SearchUser)

        [Required]
        public DateTime ActionDate { get; set; } = DateTime.UtcNow; 

        [Required]
        public string ActionDetails { get; set; }

        [ForeignKey("AdminId")]
        public virtual Users Admin { get; set; }

        [ForeignKey("TargetUserId")]
        public virtual Users TargetUser { get; set; }

    }
}
