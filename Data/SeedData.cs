// using Microsoft.AspNetCore.Identity;
// using Microsoft.EntityFrameworkCore;
// using SinhVien5TotWeb.Models;

// namespace SinhVien5TotWeb.Data
// {
//     public static class SeedData
//     {
//         public static async Task Initialize(IServiceProvider serviceProvider)
//         {
//             using var context = new AppDbContext(
//                 serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

//             // Create roles if they don't exist
//             var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//             var roles = new[] { "Student", "FacultyOfficer", "UniversityOfficer", "CityOfficer", "CentralOfficer" };

//             foreach (var role in roles)
//             {
//                 if (!await roleManager.RoleExistsAsync(role))
//                 {
//                     await roleManager.CreateAsync(new IdentityRole(role));
//                 }
//             }

//             // Create users if they don't exist
//             var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

//             // Faculty Officers
//             var facultyOfficers = new[]
//             {
//                 new { Email = "faculty1@example.com", Name = "Cán bộ Khoa 1", Faculty = "Công nghệ thông tin" },
//                 new { Email = "faculty2@example.com", Name = "Cán bộ Khoa 2", Faculty = "Công nghệ thông tin" },
//                 new { Email = "faculty3@example.com", Name = "Cán bộ Khoa 3", Faculty = "Điện tử viễn thông" }
//             };

//             foreach (var officer in facultyOfficers)
//             {
//                 if (await userManager.FindByEmailAsync(officer.Email) == null)
//                 {
//                     var user = new IdentityUser
//                     {
//                         UserName = officer.Email,
//                         Email = officer.Email,
//                         EmailConfirmed = true
//                     };

//                     var result = await userManager.CreateAsync(user, "Password123!");
//                     if (result.Succeeded)
//                     {
//                         await userManager.AddToRoleAsync(user, "FacultyOfficer");
                        
//                         // Create officer profile
//                         var officerProfile = new Officer
//                         {
//                             UserId = user.Id,
//                             FullName = officer.Name,
//                             Email = officer.Email,
//                             Level = OfficerLevel.Faculty,
//                             Faculty = officer.Faculty,
//                             IsActive = true
//                         };
//                         context.Officers.Add(officerProfile);
//                     }
//                 }
//             }

//             // University Officer
//             var universityOfficer = new { Email = "university@example.com", Name = "Cán bộ Trường" };
//             if (await userManager.FindByEmailAsync(universityOfficer.Email) == null)
//             {
//                 var user = new IdentityUser
//                 {
//                     UserName = universityOfficer.Email,
//                     Email = universityOfficer.Email,
//                     EmailConfirmed = true
//                 };

//                 var result = await userManager.CreateAsync(user, "Password123!");
//                 if (result.Succeeded)
//                 {
//                     await userManager.AddToRoleAsync(user, "UniversityOfficer");
                    
//                     var officerProfile = new Officer
//                     {
//                         UserId = user.Id,
//                         FullName = universityOfficer.Name,
//                         Email = universityOfficer.Email,
//                         Level = OfficerLevel.University,
//                         IsActive = true
//                     };
//                     context.Officers.Add(officerProfile);
//                 }
//             }

//             // City Officer
//             var cityOfficer = new { Email = "city@example.com", Name = "Cán bộ Thành phố" };
//             if (await userManager.FindByEmailAsync(cityOfficer.Email) == null)
//             {
//                 var user = new IdentityUser
//                 {
//                     UserName = cityOfficer.Email,
//                     Email = cityOfficer.Email,
//                     EmailConfirmed = true
//                 };

//                 var result = await userManager.CreateAsync(user, "Password123!");
//                 if (result.Succeeded)
//                 {
//                     await userManager.AddToRoleAsync(user, "CityOfficer");
                    
//                     var officerProfile = new Officer
//                     {
//                         UserId = user.Id,
//                         FullName = cityOfficer.Name,
//                         Email = cityOfficer.Email,
//                         Level = OfficerLevel.City,
//                         IsActive = true
//                     };
//                     context.Officers.Add(officerProfile);
//                 }
//             }

//             // Central Officer
//             var centralOfficer = new { Email = "central@example.com", Name = "Cán bộ Trung ương" };
//             if (await userManager.FindByEmailAsync(centralOfficer.Email) == null)
//             {
//                 var user = new IdentityUser
//                 {
//                     UserName = centralOfficer.Email,
//                     Email = centralOfficer.Email,
//                     EmailConfirmed = true
//                 };

//                 var result = await userManager.CreateAsync(user, "Password123!");
//                 if (result.Succeeded)
//                 {
//                     await userManager.AddToRoleAsync(user, "CentralOfficer");
                    
//                     var officerProfile = new Officer
//                     {
//                         UserId = user.Id,
//                         FullName = centralOfficer.Name,
//                         Email = centralOfficer.Email,
//                         Level = OfficerLevel.Central,
//                         IsActive = true
//                     };
//                     context.Officers.Add(officerProfile);
//                 }
//             }

//             await context.SaveChangesAsync();
//         }
//     }
// } 