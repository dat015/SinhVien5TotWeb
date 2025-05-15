using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SinhVien5TotWeb.Models
{
    public class ApprovalDecision //Quyết định xét duyệt    
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("ApplicationId")]
        public Application Application { get; set; } = null!;
        public int ApplicationId { get; set; }
        public int OfficerId { get; set; }

        [Required]
        [ForeignKey("OfficerId")]
        public Officer Officer { get; set; } = null!;

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, NeedMore, Approved, Rejected

        [Required]
        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }

    public enum DecisionStatus
    {
        Approved,
        Rejected,
        Pending
    }
}