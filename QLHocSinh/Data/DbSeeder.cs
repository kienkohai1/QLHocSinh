using Microsoft.AspNetCore.Identity;
using QLHocSinh.Models;

namespace QLHocSinh.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            // Lấy các service cần thiết
            var userManager = service.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Khởi tạo các Role (nếu chưa tồn tại)
            string[] roleNames = { "Admin", "Teacher", "Parent" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Tạo tài khoản Admin mặc định
            var adminEmail = "admin@school.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    UserRole = "Admin",
                    FullName = "Ban Giám Hiệu"
                };

                // Đặt mật khẩu mặc định (ví dụ: Admin@123)
                var result = await userManager.CreateAsync(newAdmin, "Admin@123");

                if (result.Succeeded)
                {
                    // Gán quyền Admin cho tài khoản này
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}