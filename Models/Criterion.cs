using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SinhVien5TotWeb.Models
{
    public class Criterion //Tiêu chí
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Tên tiêu chí không được vượt quá 50 ký tự")]
        [Display(Name = "Tên tiêu chí")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Điểm tối đa là bắt buộc")]
        [Range(0, 100, ErrorMessage = "Điểm tối đa phải từ 0 đến 100")]
        [Display(Name = "Điểm tối đa")]
        public int MaxScore { get; set; } = 100;

        public int MinScore { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Quan hệ: Một tiêu chí có thể xuất hiện trong nhiều minh chứng và kết quả chấm điểm
        public virtual ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();
        public virtual ICollection<ScoringResult> ScoringResults { get; set; } = new List<ScoringResult>();
        public virtual ICollection<ApplicationCriterion> ApplicationCriteria { get; set; } = new List<ApplicationCriterion>();
    }
    public enum ApplicationLevel
    {
        Faculty,
        University,
        Province,
        National
    }

    public enum ApplicationStatus
    {
        Pending,
        SupplementRequested,
        Approved,
        Rejected
    }
}