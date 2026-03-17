using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLHocSinh.Models;
using QLHocSinh.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLHocSinh.Controllers
{
    [Authorize] // Yêu cầu đăng nhập
    public class GradeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GradeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Chọn lớp, môn, loại điểm
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.ListClasses = new SelectList(
                await _context.Classes.OrderBy(c => c.ClassName).ToListAsync(),
                "Id", "ClassName");   // giả sử property là ClassName

            ViewBag.ListSubjects = new SelectList(
                await _context.Subjects.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name");

            var examTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Miệng",   Text = "Miệng"   },
                new SelectListItem { Value = "15p",     Text = "15 phút" },
                new SelectListItem { Value = "Giữa kỳ", Text = "Giữa kỳ" },
                new SelectListItem { Value = "Cuối kỳ", Text = "Cuối kỳ" }
            };
            ViewBag.ListExamTypes = examTypes;

            return View();
        }

        // GET: Hiển thị form nhập điểm
        [HttpGet]
        public async Task<IActionResult> Enter(int classId, int subjectId, string examType)
        {
            if (classId <= 0 || subjectId <= 0 || string.IsNullOrWhiteSpace(examType))
            {
                TempData["ErrorMessage"] = "Thông tin lớp, môn hoặc loại điểm không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra examType hợp lệ
            var validExamTypes = new[] { "Miệng", "15p", "Giữa kỳ", "Cuối kỳ" };
            if (!validExamTypes.Contains(examType))
            {
                TempData["ErrorMessage"] = "Loại điểm không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var lop = await _context.Classes.FindAsync(classId);
            var mon = await _context.Subjects.FindAsync(subjectId);

            if (lop == null || mon == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lớp học hoặc môn học.";
                return RedirectToAction(nameof(Index));
            }

            // (Tùy chọn) Kiểm tra quyền: giáo viên có dạy lớp + môn này không?
            // var currentUserId = _userManager.GetUserId(User);
            // var isAssigned = await _context.TeacherAssignments
            //     .AnyAsync(a => a.TeacherId == currentUserId && a.ClassId == classId && a.SubjectId == subjectId);
            // if (!isAssigned && !User.IsInRole("Admin"))
            // {
            //     TempData["ErrorMessage"] = "Bạn không được phân công dạy lớp/môn này.";
            //     return RedirectToAction(nameof(Index));
            // }

            var students = await _context.Students
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            var existingGrades = await _context.Grades
                .Where(g => g.SubjectId == subjectId &&
                            g.ExamType == examType &&
                            g.Student.ClassId == classId)
                .ToDictionaryAsync(g => g.StudentId, g => g.Score);

            var model = new GradeEntryViewModel
            {
                ClassId = classId,
                SubjectId = subjectId,
                ExamType = examType,
                ClassName = lop.ClassName ?? "Không xác định",
                SubjectName = mon.Name ?? "Không xác định",
                Students = students.Select(s => new StudentScoreItem
                {
                    StudentId = s.Id,
                    StudentCode = s.StudentCode,
                    FullName = s.FullName,
                    Score = existingGrades.TryGetValue(s.Id, out var score) ? score : null
                }).ToList()
            };

            return View(model);
        }

        // POST: Lưu điểm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrades(GradeEntryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Nếu validation lỗi → trả về view với lỗi
                var lop = await _context.Classes.FindAsync(model.ClassId);
                var mon = await _context.Subjects.FindAsync(model.SubjectId);

                if (lop != null) model.ClassName = lop.ClassName;
                if (mon != null) model.SubjectName = mon.Name;

                return View("Enter", model);
            }

            var currentUserId = _userManager.GetUserId(User);
            var validExamTypes = new[] { "Miệng", "15p", "Giữa kỳ", "Cuối kỳ" };

            if (!validExamTypes.Contains(model.ExamType))
            {
                ModelState.AddModelError("", "Loại điểm không hợp lệ.");
                return View("Enter", model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in model.Students)
                {
                    var existing = await _context.Grades
                        .FirstOrDefaultAsync(g => g.StudentId == item.StudentId &&
                                                  g.SubjectId == model.SubjectId &&
                                                  g.ExamType == model.ExamType);

                    if (item.Score.HasValue)
                    {
                        // Có điểm → cập nhật hoặc thêm mới
                        if (existing != null)
                        {
                            existing.Score = item.Score.Value;
                            existing.TeacherId = currentUserId;
                            existing.DateCreated = DateTime.Now;
                            _context.Grades.Update(existing);
                        }
                        else
                        {
                            var newGrade = new Grade
                            {
                                StudentId = item.StudentId,
                                SubjectId = model.SubjectId,
                                ExamType = model.ExamType,
                                Score = item.Score.Value,
                                TeacherId = currentUserId,
                                DateCreated = DateTime.Now
                            };
                            _context.Grades.Add(newGrade);
                        }
                    }
                    else if (existing != null)
                    {
                        // Không có điểm (null) nhưng trước đó có → xóa bản ghi cũ
                        _context.Grades.Remove(existing);
                    }
                    // Nếu chưa có và Score vẫn null → bỏ qua (không làm gì)
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Đã lưu điểm {model.ExamType} môn {model.SubjectName} lớp {model.ClassName} thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Lỗi khi lưu điểm: " + ex.Message);
                return View("Enter", model);
            }
        }
    }
}