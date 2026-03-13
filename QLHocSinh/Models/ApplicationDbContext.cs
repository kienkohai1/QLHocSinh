using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QLHocSinh.Models; // Đảm bảo namespace này khớp với nơi bạn để các Model

namespace QLHocSinh.Models
{
    // IdentityDbContext giúp tích hợp sẵn các bảng User và Role
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Assignment> Assignments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Cấu hình cho bảng Grade: Khi xóa Student, không tự động xóa Grade (chống vòng lặp)
            builder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // Thay đổi ở đây

            // 2. Cấu hình tương tự nếu bảng Grade có liên kết với Teacher
            builder.Entity<Grade>()
                .HasOne(g => g.Teacher)
                .WithMany()
                .HasForeignKey(g => g.TeacherId)
                .OnDelete(DeleteBehavior.Restrict); // Thay đổi ở đây

            // 3. Nếu bảng Assignment cũng gây lỗi tương tự
            builder.Entity<Assignment>()
                .HasOne(a => a.Teacher)
                .WithMany()
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}