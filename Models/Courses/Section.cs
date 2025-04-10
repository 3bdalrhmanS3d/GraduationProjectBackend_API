﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraduationProjectBackendAPI.Models.Courses
{
    public class Section
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SectionId { get; set; } // Primary Key

        [ForeignKey("Level")]
        public int LevelId { get; set; } // Foreign Key
        public string SectionName { get; set; }
        public int SectionOrder { get; set; }


        // Navigation Properties
        public virtual Level Level { get; set; }
        public ICollection<Content>? Contents { get; set; }
        public virtual Quiz Quiz { get; set; }
        public virtual TaskT Task { get; set; }
    }
}
