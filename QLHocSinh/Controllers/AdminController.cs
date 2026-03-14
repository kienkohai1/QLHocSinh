using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public IActionResult CreateUser()
    {
        var model = new CreateUserViewModel
        {
            // Lấy danh sách Role từ DB đổ vào dropdown
            RoleList = _roleManager.Roles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name
            }).ToList()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                // THÊM DÒNG NÀY ĐỂ TRÁNH LỖI NULL TRONG DB
                UserRole = model.SelectedRole
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Gán Role đã chọn cho User vừa tạo
                await _userManager.AddToRoleAsync(user, model.SelectedRole);
                return RedirectToAction("Index", "Home"); // Hoặc trang danh sách người dùng
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        // Nếu lỗi, nạp lại danh sách Role trước khi trả về View
        model.RoleList = _roleManager.Roles.Select(r => new SelectListItem { Text = r.Name, Value = r.Name }).ToList();
        return View(model);
    }
}