using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QLHocSinh.Models;

namespace QLHocSinh.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Thực hiện đăng nhập
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.Username);
                    var roles = await _userManager.GetRolesAsync(user);

                    // Điều hướng dựa trên vai trò
                    if (roles.Contains("Admin"))
                        return RedirectToAction("Index", "Admin"); // Giả định bạn có AdminController

                    if (roles.Contains("Teacher"))
                        return RedirectToAction("MyClasses", "Teacher"); // Giả định bạn có TeacherController

                    if (roles.Contains("Parent"))
                        return RedirectToAction("ViewGrades", "Student"); // Giả định bạn có StudentController

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}