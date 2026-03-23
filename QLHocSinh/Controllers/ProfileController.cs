using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLHocSinh.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace QLHocSinh.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? academicYear, int? semester)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Subject)
                .FirstOrDefaultAsync(s => s.ParentId == userId);

            if (student == null)
            {
                return NotFound("Tài khoản của bạn chưa được liên kết với hồ sơ học sinh nào.");
            }

            // Áp dụng bộ lọc
            if (!string.IsNullOrEmpty(academicYear))
            {
                student.Grades = student.Grades.Where(g => g.AcademicYear == academicYear).ToList();
            }
            if (semester.HasValue && semester.Value > 0)
            {
                student.Grades = student.Grades.Where(g => g.Semester == semester.Value).ToList();
            }

            ViewBag.CurrentYear = academicYear;
            ViewBag.CurrentSemester = semester;

            return View(student);
        }
    }
}