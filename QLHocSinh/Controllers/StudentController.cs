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

        // 1. DANH SÁCH HỌC SINH (Đã thêm lọc theo Lớp và Tìm kiếm)
        [HttpGet]
        public async Task<IActionResult> Index(int? classId, string? searchString)
        {
            var query = _context.Students
                .Include(s => s.Class)
                .Include(s => s.Parent)
                .AsQueryable();

            if (classId.HasValue && classId > 0)
            {
                query = query.Where(s => s.ClassId == classId);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.FullName.Contains(searchString) || s.StudentCode.Contains(searchString));
            }

            ViewBag.Classes = new SelectList(await _context.Classes.ToListAsync(), "Id", "ClassName", classId);
            ViewBag.CurrentSearch = searchString;

            var students = await query.ToListAsync();
            return View(students);
        }

        // 2. CHI TIẾT HỌC SINH (Đã thêm lọc điểm theo Năm học, Học kỳ)
        public async Task<IActionResult> Details(int? id, string? academicYear, int? semester)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null) return NotFound();

            // Lọc điểm trong bộ nhớ
            if (!string.IsNullOrEmpty(academicYear))
            {
                student.Grades = student.Grades.Where(g => g.AcademicYear == academicYear).ToList();
            }
            if (semester.HasValue && semester.Value > 0)
            {
                student.Grades = student.Grades.Where(g => g.Semester == semester.Value).ToList();
            }

            ViewBag.Subjects = new SelectList(await _context.Subjects.ToListAsync(), "Id", "Name");
            ViewBag.CurrentYear = academicYear;
            ViewBag.CurrentSemester = semester;

            return View(student);
        }

        // POST: Xử lý form từ Modal Thêm Môn Học (Cập nhật lưu theo Năm và Học kỳ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubject(int studentId, int subjectId, string academicYear, int semester)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            var alreadyExists = await _context.Grades
                .AnyAsync(g => g.StudentId == studentId && g.SubjectId == subjectId && g.AcademicYear == academicYear && g.Semester == semester);

            if (!alreadyExists)
            {
                var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var gradePlaceholder = new Grade
                {
                    StudentId = studentId,
                    SubjectId = subjectId,
                    AcademicYear = academicYear,  // Thêm mới
                    Semester = semester,          // Thêm mới
                    ExamType = "Khởi tạo",
                    Score = 0,
                    DateCreated = DateTime.Now,
                    TeacherId = teacherId ?? ""
                };

                _context.Grades.Add(gradePlaceholder);
                await _context.SaveChangesAsync();
            }

            // Quay lại trang Details và giữ nguyên bộ lọc
            return RedirectToAction(nameof(Details), new { id = studentId, academicYear, semester });
        }
        // 3. THÊM MỚI HỌC SINH (GET)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PrepareViewData();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, string? NewParentFullName, string? NewParentEmail)
        {
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
                        UserRole = "Parent"
                    };

                    var result = await _userManager.CreateAsync(newParent, "Parent@123");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(newParent, "Parent");
                        student.ParentId = newParent.Id;
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
                    student.ParentId = existingUser.Id;
                }
            }

            if (_context.Students.Any(s => s.StudentCode == student.StudentCode))
            {
                ModelState.AddModelError("StudentCode", "Mã học sinh này đã tồn tại.");
            }

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

            await PrepareViewData(student.ClassId, student.ParentId);
            return View(student);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();
            await PrepareViewData(student.ClassId, student.ParentId);
            return View(student);
        }

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