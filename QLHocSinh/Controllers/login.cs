using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QLHocSinh.Models;

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
            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                var roles = await _userManager.GetRolesAsync(user);

                // Điều hướng dựa trên Role
                if (roles.Contains("Admin"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

                if (roles.Contains("Teacher"))
                    return RedirectToAction("MyClasses", "Teacher");

                if (roles.Contains("Parent"))
                    return RedirectToAction("StudentDetail", "Parent");

                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
        }
        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }
}