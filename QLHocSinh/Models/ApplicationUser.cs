using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }

    // Thêm dấu ? để cho phép null trong Database
    public string? Address { get; set; }

    public string UserRole { get; set; }
}