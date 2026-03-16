using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLHocSinh.Models;

namespace QLHocSinh.Controllers
{
    // Chỉ Admin mới có quyền quản lý môn học
    [Authorize(Roles = "Admin")]
    public class SubjectController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH MÔN HỌC (INDEX)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var subjects = await _context.Subjects.ToListAsync();
            return View(subjects);
        }

        // ==========================================
        // 2. XEM CHI TIẾT MÔN HỌC (DETAILS)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subject == null) return NotFound();

            return View(subject);
        }

        // ==========================================
        // 3. THÊM MỚI MÔN HỌC (CREATE)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Credits")] Subject subject)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem tên môn học đã tồn tại chưa
                bool isExist = await _context.Subjects.AnyAsync(s => s.Name.ToLower() == subject.Name.ToLower());
                if (isExist)
                {
                    ModelState.AddModelError("Name", "Tên môn học này đã tồn tại trong hệ thống.");
                    return View(subject);
                }

                _context.Add(subject);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(subject);
        }

        // ==========================================
        // 4. CHỈNH SỬA MÔN HỌC (EDIT)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();

            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Credits")] Subject subject)
        {
            if (id != subject.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra trùng tên môn học (ngoại trừ chính nó)
                    bool isExist = await _context.Subjects.AnyAsync(s => s.Name.ToLower() == subject.Name.ToLower() && s.Id != id);
                    if (isExist)
                    {
                        ModelState.AddModelError("Name", "Tên môn học này đã tồn tại trong hệ thống.");
                        return View(subject);
                    }

                    _context.Update(subject);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubjectExists(subject.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(subject);
        }

        // ==========================================
        // 5. XÓA MÔN HỌC (DELETE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subject == null) return NotFound();

            // KIỂM TRA RÀNG BUỘC: Môn học đã có điểm hoặc đã phân công giáo viên chưa?
            bool hasGrades = await _context.Grades.AnyAsync(g => g.SubjectId == id);
            bool hasAssignments = await _context.Assignments.AnyAsync(a => a.SubjectId == id);

            ViewBag.CanDelete = !(hasGrades || hasAssignments);

            if (!ViewBag.CanDelete)
            {
                ViewBag.ErrorMessage = "Không thể xóa môn học này vì đã có dữ liệu Điểm số hoặc Phân công giảng dạy liên quan. Vui lòng xóa dữ liệu liên quan trước.";
            }

            return View(subject);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject != null)
            {
                // Kiểm tra lại lần nữa trước khi xóa thực sự
                bool hasGrades = await _context.Grades.AnyAsync(g => g.SubjectId == id);
                bool hasAssignments = await _context.Assignments.AnyAsync(a => a.SubjectId == id);

                if (hasGrades || hasAssignments)
                {
                    // Nếu cố tình gửi request xóa khi không được phép
                    TempData["Error"] = "Thao tác thất bại. Môn học đang bị ràng buộc dữ liệu.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Hàm hỗ trợ kiểm tra sự tồn tại của môn học
        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.Id == id);
        }
    }
}