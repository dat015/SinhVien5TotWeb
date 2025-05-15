using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using SinhVien5TotWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace SinhVien5TotWeb.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            if (!context.Students.Any())
            {
                var students = new List<Student>
    {
        new Student
        {
            StudentId = "B20DCCN001",
            FullName = "Nguyễn Văn A",
            Email = "a.nguyenvan@student.tlu.edu.vn",
            Password = HashPassword("123456"),
            PhoneNumber = "0911000001",
            Class = "D20CQCN01-B",
            Faculty = "Khoa Công nghệ Thông tin",
            University = "Đại học Thủy Lợi",
            CreatedAt = DateTime.Now
        },
        new Student
        {
            StudentId = "B20DCCN002",
            FullName = "Trần Thị B",
            Email = "b.tranthib@student.tlu.edu.vn",
            Password = HashPassword("123456"),
            PhoneNumber = "0911000002",
            Class = "D20CQCN02-B",
            Faculty = "Khoa Công nghệ Thông tin",
            University = "Đại học Thủy Lợi",
            CreatedAt = DateTime.Now
        },
        new Student
        {
            StudentId = "B20DCCN003",
            FullName = "Lê Văn C",
            Email = "c.levan@student.tlu.edu.vn",
            Password = HashPassword("123456"),
            PhoneNumber = "0911000003",
            Class = "D20CQCN03-B",
            Faculty = "Khoa Công nghệ Thông tin",
            University = "Đại học Thủy Lợi",
            CreatedAt = DateTime.Now
        }
    };

                context.Students.AddRange(students);
                context.SaveChanges();
            }

            // Kiểm tra xem đã có dữ liệu trong bảng Officer chưa
            if (context.Officers.Any())
            {
                return; // Bảng Officer đã có dữ liệu
            }

            // Danh sách tài khoản cán bộ
            var officers = new List<Officer>
            {
                // Cấp khoa (Faculty) - 3 cán bộ chấm điểm, 1 cán bộ xét duyệt
                new Officer
                {
                    FullName = "Nguyễn Văn Hùng",
                    Email = "hungnv@khoa.tlu.edu.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0987654321",
                    Position = "Bí thư Đoàn khoa",
                    Department = "Khoa Công nghệ Thông tin, Đại học Thủy Lợi",
                    Level = OfficerLevel.Faculty,
                    Role = OfficerRole.Scorer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Officer
                {
                    FullName = "Trần Thị Mai",
                    Email = "maitt@khoa.tlu.edu.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0912345678",
                    Position = "Phó Bí thư Đoàn khoa",
                    Department = "Khoa Công nghệ Thông tin, Đại học Thủy Lợi",
                    Level = OfficerLevel.Faculty,
                    Role = OfficerRole.Scorer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Officer
                {
                    FullName = "Lê Minh Tuấn",
                    Email = "tuanlm@khoa.tlu.edu.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0934567890",
                    Position = "Ủy viên Ban Chấp hành Đoàn khoa",
                    Department = "Khoa Công nghệ Thông tin, Đại học Thủy Lợi",
                    Level = OfficerLevel.Faculty,
                    Role = OfficerRole.Scorer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Officer
                {
                    FullName = "Phạm Thị Lan",
                    Email = "lanpt@khoa.tlu.edu.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0971234567",
                    Position = "Chủ nhiệm Khoa",
                    Department = "Khoa Công nghệ Thông tin, Đại học Thủy Lợi",
                    Level = OfficerLevel.Faculty,
                    Role = OfficerRole.Approver,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },

                // Cấp trường (University) - 2 cán bộ chấm điểm, 1 cán bộ xét duyệt
                new Officer
                {
                    FullName = "Hoàng Văn Nam",
                    Email = "namhv@truong.tlu.edu.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0967891234",
                    Position = "Bí thư Đoàn trường",
                    Department = "Đoàn Thanh niên, Đại học Thủy Lợi",
                    Level = OfficerLevel.University,
                    Role = OfficerRole.Scorer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Officer
                {
                    FullName = "Nguyễn Thị Hồng",
                    Email = "hongnt@truong.tlu.edu.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0945678901",
                    Position = "Chủ tịch Hội Sinh viên",
                    Department = "Hội Sinh viên, Đại học Thủy Lợi",
                    Level = OfficerLevel.University,
                    Role = OfficerRole.Scorer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Officer
                {
                    FullName = "Vũ Minh Đức",
                    Email = "ducvm@truong.tlu.edu.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0923456789",
                    Position = "Phó Hiệu trưởng",
                    Department = "Ban Giám hiệu, Đại học Thủy Lợi",
                    Level = OfficerLevel.University,
                    Role = OfficerRole.Approver,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },

                // Cấp tỉnh/thành phố (Province) - 2 cán bộ chấm điểm, 1 cán bộ xét duyệt
                new Officer
                {
                    FullName = "Đỗ Thị Thanh",
                    Email = "thanhdt@tphcm.doanthanhnien.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0918765432",
                    Position = "Ủy viên Ban Chấp hành",
                    Department = "Thành Đoàn TP.HCM",
                    Level = OfficerLevel.Province,
                    Role = OfficerRole.Scorer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Officer
                {
                    FullName = "Phạm Văn Long",
                    Email = "longpv@tphcm.doanthanhnien.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0981234567",
                    Position = "Trưởng Ban Thanh niên Trường học",
                    Department = "Thành Đoàn TP.HCM",
                    Level = OfficerLevel.Province,
                    Role = OfficerRole.Scorer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Officer
                {
                    FullName = "Nguyễn Thị Minh Anh",
                    Email = "anhntm@tphcm.doanthanhnien.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0937894561",
                    Position = "Bí thư Thành Đoàn",
                    Department = "Thành Đoàn TP.HCM",
                    Level = OfficerLevel.Province,
                    Role = OfficerRole.Approver,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },

                // Cấp Trung ương (National) - 2 cán bộ chấm điểm, 1 cán bộ xét duyệt
                new Officer
                {
                    FullName = "Trần Quốc Hưng",
                    Email = "hungtq@tw.doanthanhnien.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0974561234",
                    Position = "Trưởng Ban Thanh niên Trường học",
                    Department = "Trung ương Đoàn TNCS Hồ Chí Minh",
                    Level = OfficerLevel.National,
                    Role = OfficerRole.Scorer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Officer
                {
                    FullName = "Lê Thị Ngọc Ánh",
                    Email = "anhlt@tw.doanthanhnien.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0961237894",
                    Position = "Ủy viên Ban Chấp hành",
                    Department = "Trung ương Đoàn TNCS Hồ Chí Minh",
                    Level = OfficerLevel.National,
                    Role = OfficerRole.Scorer,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Officer
                {
                    FullName = "Hoàng Minh Tuấn",
                    Email = "tuanhm@tw.doanthanhnien.vn",
                    Password = HashPassword("123456"),
                    PhoneNumber = "0947891236",
                    Position = "Bí thư Trung ương Đoàn",
                    Department = "Trung ương Đoàn TNCS Hồ Chí Minh",
                    Level = OfficerLevel.National,
                    Role = OfficerRole.Approver,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            };

            // Thêm danh sách cán bộ vào DbContext
            context.Officers.AddRange(officers);
            context.SaveChanges();
        }

        // Hàm mã hóa mật khẩu bằng SHA256
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}