using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SinhVien5TotWeb.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string StudentId { get; set; } = string.Empty;

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
        [StringLength(50)]
        public string Class { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Faculty { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string University { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Quan hệ: Một sinh viên có nhiều hồ sơ
        public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}