using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SinhVien5TotWeb.Data;
using SinhVien5TotWeb.Models;

namespace SinhVien5TotWeb.Services
{
    public class ApplicationService
    {

        private readonly AppDbContext _context;

        public ApplicationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateApplicationAsync(Application application)
        {
            // Calculate academic year from SubmissionDate
            var submissionDate = application.SubmissionDate;
            var academicYear = GetAcademicYear(submissionDate);

            // Check if student already has an application in this academic year
            var existingApplication = await _context.Applications
                .Where(a => a.StudentId == application.StudentId)
                .Where(a => EF.Functions.DateDiffYear(a.SubmissionDate, submissionDate) == 0)
                .FirstOrDefaultAsync();

            if (existingApplication != null)
            {
                throw new InvalidOperationException("Sinh viên đã đăng ký danh hiệu trong năm học này.");
            }

            // Set academic year and add application
            application.AcademicYear = academicYear;
            _context.Applications.Add(application);
            await _context.SaveChangesAsync();
            return true;
        }

        public string GetAcademicYear(DateTime date)
        {
            int year = date.Month >= 9 ? date.Year : date.Year - 1;
            return $"{year}-{year + 1}";
        }
    }
}