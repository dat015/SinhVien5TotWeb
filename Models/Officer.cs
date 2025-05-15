using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SinhVien5TotWeb.Models
{
    public class Officer //Cán bộ Đoàn – Hội
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<ScoringResult> ScoringResults { get; set; } = new List<ScoringResult>();

        public ICollection<ApprovalDecision> ApprovalDecisions { get; set; } = new List<ApprovalDecision>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [Required(ErrorMessage = "Cấp quản lý là bắt buộc")]
        [Display(Name = "Cấp quản lý")]
        public OfficerLevel Level { get; set; } // Khoa, Trường, Tỉnh/TP, Trung ương

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        [Display(Name = "Vai trò")]
        public OfficerRole Role { get; set; } // Chấm điểm hoặc Xét duyệt
    }

    public enum OfficerLevel
    {
        Faculty,
        University,
        Province,
        National
    }

    public enum OfficerRole
    {
        Scorer, // Cán bộ chấm điểm
        Approver // Cán bộ xét duyệt
    }
}