using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class CreateUserViewModel
{
    [Required]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public string FullName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    public string SelectedRole { get; set; }

    // Danh sách để hiển thị lên Dropdown chọn Role
    public List<SelectListItem>? RoleList { get; set; }
}