using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SinhVien5TotWeb.Models
{
    public class ApplicationCriterion
    {
        [Key]
        public int id {get; set;}
        public int ApplicationId { get; set; }
        [ForeignKey("ApplicationId")]
        public Application Application { get; set; } = null!;
        public int CriterionId { get; set; }
        [ForeignKey("CriterionId")]
        public Criterion Criterion { get; set; } = null!;
        public DateTime AssignedAt { get; set; } = DateTime.Now;
    }
}