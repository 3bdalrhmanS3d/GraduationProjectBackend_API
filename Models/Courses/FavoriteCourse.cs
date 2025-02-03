using GraduationProjectBackendAPI.Models.User;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class FavoriteCourse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FavoriteId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("Courses")]
        public int CourseId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public virtual Users User { get; set; }
        public virtual Courses Course { get; set; }
    }
}
