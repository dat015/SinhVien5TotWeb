using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SinhVien5TotWeb.Models
{
    public class Application
    {
        [Key]
        public int ApplicationId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public ApplicationLevel CurrentLevel { get; set; } // Current processing level

        [Required]
        public ApplicationStatus Status { get; set; }

        [Required]
        public DateTime SubmissionDate { get; set; }
        [Required]
        [StringLength(9)] // e.g., "2024-2025"
        [Display(Name = "Năm học")]
        public string AcademicYear { get; set; } // e.g., "2024-2025"

        public DateTime? LastUpdated { get; set; }
        [Display(Name = "Số lần từ chối")]
        public int RejectionCount { get; set; } = 0;

        [ForeignKey("StudentId")]
        public Student Student { get; set; } = null!;

        public ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();

        public ICollection<ScoringResult> Scores { get; set; } = new List<ScoringResult>();

        public ICollection<ApplicationCriterion> ApplicationCriteria { get; set; } = new List<ApplicationCriterion>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}