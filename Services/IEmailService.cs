using SinhVien5TotWeb.Models;

namespace SinhVien5TotWeb.Services
{
    public interface IEmailService
    {
        Task SendApplicationStatusUpdateEmail(string to, string studentName, ApplicationStatus status);
        Task SendSupplementRequestEmail(string to, string studentName, string reason);
    }
} 