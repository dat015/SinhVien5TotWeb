using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SinhVien5TotWeb.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public int StudentId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = "info"; // info, success, warning, danger

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }
    }
} 