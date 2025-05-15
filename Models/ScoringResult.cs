using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SinhVien5TotWeb.Models
{
    public class ScoringResult //Kết quả chấm điểm
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("ApplicationId")]
        public Application Application { get; set; } = null!;
        public int ApplicationId { get; set; }
        public int OfficerId { get; set; }

        [Required]
        [ForeignKey("CriterionId")]
        public Criterion Criterion { get; set; } = null!;
        public int CriterionId { get; set; }

        [Required]
        [ForeignKey("OfficerId")]
        public Officer Officer { get; set; } = null!;

        [Required]
        [Range(0, 100)]
        [Display(Name = "Điểm số")]
        public double Score { get; set; }
        [Display(Name = "Cấp hiện tại")]
        public ApplicationLevel CurrentLevel { get; set; } = ApplicationLevel.Faculty; // Current processing level

        [Required]
        [StringLength(1000, ErrorMessage = "Nhận xét không được vượt quá 1000 ký tự")]
        [Display(Name = "Nhận xét")]
        public string Comment { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        [Display(Name = "Ngày chấm")]
        public DateTime ScoredAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}