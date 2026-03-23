using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using QLHocSinh.Models; // Đảm bảo đúng namespace của model

namespace QLHocSinh.Controllers
{
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

        // 1. TRANG CHỦ ADMIN (DASHBOARD)
        [HttpGet]
        public IActionResult Index()
        {
            // Ở đây bạn có thể đếm số lượng học sinh, giáo viên để truyền ra View nếu muốn
            return View();
        }

        // 2. TẠO NGƯỜI DÙNG MỚI
        [HttpGet]
        public IActionResult CreateUser()
        {
            var model = new CreateUserViewModel
            {
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
                    UserRole = model.SelectedRole
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    // Sau khi tạo xong, đưa Admin về lại Dashboard
                    return RedirectToAction("Index", "Admin");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            model.RoleList = _roleManager.Roles.Select(r => new SelectListItem { Text = r.Name, Value = r.Name }).ToList();
            return View(model);
        }
        // 3. QUẢN LÝ DANH SÁCH TÀI KHOẢN (KHÔNG DÙNG VIEWMODEL)
        [HttpGet]
        public IActionResult ManageUsers()
        {
            // Truyền thẳng danh sách ApplicationUser ra View
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // 4. CHỈNH SỬA TÀI KHOẢN
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Lấy danh sách Role đưa vào ViewBag để tạo Dropdown
            ViewBag.RoleList = _roleManager.Roles.Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name
            }).ToList();

            // Truyền thẳng ApplicationUser ra View
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Nhận trực tiếp các tham số từ Form thay vì dùng ViewModel
        public async Task<IActionResult> EditUser(string id, string FullName, string Email, string UserRole)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.FullName = FullName;
                user.Email = Email;
                user.UserRole = UserRole; // Cập nhật field trong DB của bạn

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    // Cập nhật lại Role thực tế trong Identity
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                    if (!string.IsNullOrEmpty(UserRole))
                    {
                        await _userManager.AddToRoleAsync(user, UserRole);
                    }
                    return RedirectToAction("ManageUsers");
                }
            }
            return RedirectToAction("ManageUsers");
        }

        // 5. ĐẶT LẠI MẬT KHẨU (RESET PASSWORD)
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Nhận ID và Mật khẩu mới trực tiếp từ input form
        public async Task<IActionResult> ResetPassword(string id, string NewPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null && !string.IsNullOrEmpty(NewPassword))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, resetToken, NewPassword);

                TempData["SuccessMessage"] = $"Đã đổi mật khẩu thành công cho {user.UserName}!";
            }
            return RedirectToAction("ManageUsers");
        }

        // 6. XÓA TÀI KHOẢN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                if (user.UserName == User.Identity.Name)
                {
                    TempData["ErrorMessage"] = "Không thể tự xóa chính mình!";
                    return RedirectToAction("ManageUsers");
                }
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("ManageUsers");
        }
    }
}