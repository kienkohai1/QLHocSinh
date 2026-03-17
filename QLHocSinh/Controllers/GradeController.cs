using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLHocSinh.Models;
using QLHocSinh.Models.ViewModels;

namespace QLHocSinh.Controllers
{
    [Authorize] // Yêu cầu phải đăng nhập
    public class GradeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GradeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Màn hình chọn Lớp, Môn, Loại điểm
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["ClassId"] = new SelectList(await _context.Classes.ToListAsync(), "Id", "Name");
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.ToListAsync(), "Id", "Name");

            // Tạm thời hardcode danh sách loại điểm, có thể tách thành bảng riêng sau
            ViewData["ExamType"] = new SelectList(new List<string> { "Miệng", "15p", "Giữa kỳ", "Cuối kỳ" });

            return View();
        }

        // 2. Màn hình nhập điểm cho toàn lớp
        [HttpGet]
        public async Task<IActionResult> Enter(int classId, int subjectId, string examType)
        {
            var lopHoc = await _context.Classes.FindAsync(classId);
            var monHoc = await _context.Subjects.FindAsync(subjectId);

            if (lopHoc == null || monHoc == null || string.IsNullOrEmpty(examType))
            {
                return NotFound("Không tìm thấy thông tin hợp lệ.");
            }

            // Lấy danh sách học sinh thuộc lớp này
            var students = await _context.Students
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            // Lấy điểm hiện tại (nếu có) để hiển thị lên form
            var existingGrades = await _context.Grades
                .Where(g => g.SubjectId == subjectId && g.ExamType == examType && g.Student.ClassId == classId)
                .ToDictionaryAsync(g => g.StudentId, g => g.Score);

            var model = new GradeEntryViewModel
            {
                ClassId = classId,
                SubjectId = subjectId,
                ExamType = examType,
                ClassName = lopHoc.ClassName,
                SubjectName = monHoc.Name,
                Students = students.Select(s => new StudentScoreItem
                {
                    StudentId = s.Id,
                    StudentCode = s.StudentCode,
                    FullName = s.FullName,
                    Score = existingGrades.ContainsKey(s.Id) ? existingGrades[s.Id] : null
                }).ToList()
            };

            return View(model);
        }

        // 3. Xử lý lưu điểm vào Database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrades(GradeEntryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Enter", model);
            }

            var currentUserId = _userManager.GetUserId(User); // Lấy ID giáo viên đang thao tác

            foreach (var item in model.Students)
            {
                // Chỉ xử lý những học sinh được nhập điểm
                if (item.Score.HasValue)
                {
                    var existingGrade = await _context.Grades.FirstOrDefaultAsync(g =>
                        g.StudentId == item.StudentId &&
                        g.SubjectId == model.SubjectId &&
                        g.ExamType == model.ExamType);

                    if (existingGrade != null)
                    {
                        // Cập nhật điểm nếu đã tồn tại
                        existingGrade.Score = item.Score.Value;
                        existingGrade.TeacherId = currentUserId; // Lưu vết người sửa cuối
                        existingGrade.DateCreated = DateTime.Now;
                        _context.Update(existingGrade);
                    }
                    else
                    {
                        // Thêm mới nếu chưa có
                        var newGrade = new Grade
                        {
                            StudentId = item.StudentId,
                            SubjectId = model.SubjectId,
                            ExamType = model.ExamType,
                            Score = item.Score.Value,
                            TeacherId = currentUserId,
                            DateCreated = DateTime.Now
                        };
                        _context.Add(newGrade);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu điểm thành công!";

            // Quay lại màn hình chọn
            return RedirectToAction(nameof(Index));
        }
    }
}