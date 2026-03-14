using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLHocSinh.Models; // Đảm bảo namespace này khớp với project của bạn

namespace QLHocSinh.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // 1. DANH SÁCH HỌC SINH (INDEX)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Parent)
                .ToListAsync();

            return View(students);
        }

        // ==========================================
        // 2. XEM CHI TIẾT HỌC SINH (DETAILS)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null) return NotFound();

            return View(student);
        }

        // ==========================================
        // 3. THÊM MỚI HỌC SINH (CREATE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "ClassName");

            var parents = await _userManager.GetUsersInRoleAsync("Parent");
            ViewData["ParentId"] = new SelectList(parents, "Id", "FullName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "ClassName", student.ClassId);
            var parents = await _userManager.GetUsersInRoleAsync("Parent");
            ViewData["ParentId"] = new SelectList(parents, "Id", "FullName", student.ParentId);

            return View(student);
        }

        // ==========================================
        // 4. CHỈNH SỬA THÔNG TIN HỌC SINH (EDIT)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            // Nạp dữ liệu cho Dropdown và chọn sẵn giá trị hiện tại của học sinh
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "ClassName", student.ClassId);
            var parents = await _userManager.GetUsersInRoleAsync("Parent");
            ViewData["ParentId"] = new SelectList(parents, "Id", "FullName", student.ParentId);

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "ClassName", student.ClassId);
            var parents = await _userManager.GetUsersInRoleAsync("Parent");
            ViewData["ParentId"] = new SelectList(parents, "Id", "FullName", student.ParentId);

            return View(student);
        }

        // ==========================================
        // 5. XÓA HỌC SINH (DELETE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null) return NotFound();

            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Hàm hỗ trợ kiểm tra học sinh có tồn tại hay không (dùng trong hàm Edit)
        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}