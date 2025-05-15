using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SinhVien5TotWeb.Models
{
    public class Evidence //Minh chứng
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Application Application { get; set; } = null!;

        [Required]
        public Criterion Criterion { get; set; } = null!;

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}