using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Address { get; set; }

    // Thuộc tính để xác định vai trò nhanh (tùy chọn)
    public string UserRole { get; set; }
}