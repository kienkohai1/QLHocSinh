using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLHocSinh.Models;

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

        // 1. DANH SÁCH HỌC SINH
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Parent)
                .ToListAsync();
            return View(students);
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null) return NotFound();

            // Chỉ cần chuẩn bị danh sách Môn học
            ViewBag.Subjects = new SelectList(await _context.Subjects.ToListAsync(), "Id", "Name");

            return View(student);
        }

        // POST: Xử lý form từ Modal Thêm Môn Học
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubject(int studentId, int subjectId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Kiểm tra xem học sinh đã có môn học này trong bảng điểm chưa để tránh thêm trùng
            var alreadyExists = await _context.Grades
                .AnyAsync(g => g.StudentId == studentId && g.SubjectId == subjectId);

            if (!alreadyExists)
            {
                var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Tạo bản ghi "mồi" để hiển thị môn học lên bảng
                var gradePlaceholder = new Grade
                {
                    StudentId = studentId,
                    SubjectId = subjectId,
                    ExamType = "Khởi tạo", // Loại điểm ảo, không hiển thị ra các cột điểm thực tế
                    Score = 0,
                    DateCreated = DateTime.Now,
                    TeacherId = teacherId ?? ""
                };

                _context.Grades.Add(gradePlaceholder);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = studentId });
        }

        // 3. THÊM MỚI HỌC SINH (GET)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PrepareViewData();
            return View();
        }

        // 3. THÊM MỚI HỌC SINH (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, string? NewParentFullName, string? NewParentEmail)
        {
            // Bước A: Xử lý tạo tài khoản phụ huynh mới nếu có thông tin nhập vào
            if (!string.IsNullOrEmpty(NewParentEmail) && !string.IsNullOrEmpty(NewParentFullName))
            {
                var existingUser = await _userManager.FindByEmailAsync(NewParentEmail);
                if (existingUser == null)
                {
                    var newParent = new ApplicationUser
                    {
                        UserName = NewParentEmail,
                        Email = NewParentEmail,
                        FullName = NewParentFullName,
                        UserRole = "Parent" // Gán giá trị tránh lỗi SqlException
                    };

                    var result = await _userManager.CreateAsync(newParent, "Parent@123");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(newParent, "Parent");
                        student.ParentId = newParent.Id; // Gán ID vừa tạo cho học sinh
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", "Lỗi tạo phụ huynh: " + error.Description);
                        }
                    }
                }
                else
                {
                    student.ParentId = existingUser.Id; // Nếu email đã tồn tại, dùng ID đó
                }
            }

            // Bước B: Kiểm tra trùng mã học sinh
            if (_context.Students.Any(s => s.StudentCode == student.StudentCode))
            {
                ModelState.AddModelError("StudentCode", "Mã học sinh này đã tồn tại.");
            }

            // Bước C: Loại bỏ Validation cho các Object liên kết (vì form chỉ gửi ID)
            ModelState.Remove("Parent");
            ModelState.Remove("Class");
            ModelState.Remove("Grades");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi lưu học sinh: " + ex.Message);
                }
            }

            // Nếu dữ liệu không hợp lệ, nạp lại Dropdown và trả về View
            await PrepareViewData(student.ClassId, student.ParentId);
            return View(student);
        }

        // 4. CHỈNH SỬA (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            await PrepareViewData(student.ClassId, student.ParentId);
            return View(student);
        }

        // 4. CHỈNH SỬA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.Id) return NotFound();

            ModelState.Remove("Parent");
            ModelState.Remove("Class");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id)) return NotFound();
                    else throw;
                }
            }
            await PrepareViewData(student.ClassId, student.ParentId);
            return View(student);
        }

        // 5. XÓA (GET)
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

        // 5. XÓA (POST)
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

        // HÀM HỖ TRỢ: Nạp danh sách cho Dropdown
        private async Task PrepareViewData(int? selectedClass = null, string? selectedParent = null)
        {
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "ClassName", selectedClass);

            var parents = await _userManager.GetUsersInRoleAsync("Parent");
            ViewData["ParentId"] = new SelectList(parents, "Id", "FullName", selectedParent);
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}