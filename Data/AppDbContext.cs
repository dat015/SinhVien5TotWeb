using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SinhVien5TotWeb.Models;

namespace SinhVien5TotWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Officer> Officers { get; set; }
        public DbSet<Criterion> Criteria { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Evidence> Evidences { get; set; }
        public DbSet<ScoringResult> ScoringResults { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ApprovalDecision> ApprovalDecisions { get; set; }
        public DbSet<ApplicationCriterion> ApplicationCriterias {get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Student
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Email)
                .IsUnique();
            // Unique constraint for StudentId and AcademicYear
            modelBuilder.Entity<Application>()
                .HasIndex(a => new { a.StudentId, a.AcademicYear })
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.StudentId)
                .IsUnique();

            // Configure Officer
            modelBuilder.Entity<Officer>()
                .HasIndex(o => o.Email)
                .IsUnique();


            // Configure Application
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Applications)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Notification
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Student)
                .WithMany()
                .HasForeignKey(n => n.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ApplicationCriterion>()
    .HasKey(ac => new { ac.ApplicationId, ac.CriterionId });

            modelBuilder.Entity<ApplicationCriterion>()
                .HasOne(ac => ac.Application)
                .WithMany(a => a.ApplicationCriteria)
                .HasForeignKey(ac => ac.ApplicationId);

            modelBuilder.Entity<ApplicationCriterion>()
                .HasOne(ac => ac.Criterion)
                .WithMany(c => c.ApplicationCriteria)
                .HasForeignKey(ac => ac.CriterionId);
        }
    }
}