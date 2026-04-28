using Erasmus_SSC.Models;
using Microsoft.EntityFrameworkCore;

namespace Erasmus_SSC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<UserFile> UserFiles { get; set; } = null!;
        public DbSet<Download> Downloads { get; set; } = null!;
        public DbSet<News> News { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<ReportLanguage> ReportLanguages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<UserRole>().ToTable("UserRoles");
            modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");
            modelBuilder.Entity<Report>().ToTable("Reports");
            modelBuilder.Entity<ReportLanguage>().ToTable("ReportLanguages");

            modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId);


            modelBuilder.Entity<RefreshToken>()
             .HasOne(rt => rt.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(rt => rt.UserId);


            modelBuilder.Entity<UserFile>()
                .HasOne(f => f.OwnerUser)
                .WithMany(u => u.UserFiles)
                .HasForeignKey(f => f.OwnerUserId);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Language)
                .WithMany(l => l.Reports)
                .HasForeignKey(r => r.LanguageId);

            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { Id = 1, RoleName = "Admin" },
                new UserRole { Id = 2, RoleName = "User" }
            );

            modelBuilder.Entity<ReportLanguage>().HasData(
                new ReportLanguage { Id = 1, Name = "English", Code = "en" },
                new ReportLanguage { Id = 2, Name = "Danish", Code = "da" },
                new ReportLanguage { Id = 3, Name = "Norwegian", Code = "no" },
                new ReportLanguage { Id = 4, Name = "Dutch", Code = "nl" },
                new ReportLanguage { Id = 5, Name = "Finnish", Code = "fi" },
                new ReportLanguage { Id = 6, Name = "Estonian", Code = "et" }
            );
        }
    }

}

