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
    [Authorize(Roles = "Admin,Teacher")]
    public class GradeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GradeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.ListClasses = new SelectList(
                await _context.Classes.OrderBy(c => c.ClassName).ToListAsync(),
                "Id", "ClassName");

            ViewBag.ListSubjects = new SelectList(
                await _context.Subjects.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GradeBook(int classId, int subjectId, string academicYear, int semester)
        {
            if (classId <= 0 || subjectId <= 0 || string.IsNullOrEmpty(academicYear) || semester <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn đầy đủ lớp, môn học, năm học và học kỳ.";
                return RedirectToAction(nameof(Index));
            }

            var lop = await _context.Classes.FindAsync(classId);
            var mon = await _context.Subjects.FindAsync(subjectId);

            if (lop == null || mon == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lớp hoặc môn học.";
                return RedirectToAction(nameof(Index));
            }

            var students = await _context.Students
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            // Lấy tất cả điểm của môn này trong lớp (thuộc năm học và học kỳ đang chọn)
            var allGrades = await _context.Grades
                .Where(g => g.SubjectId == subjectId &&
                            g.Student.ClassId == classId &&
                            g.AcademicYear == academicYear &&
                            g.Semester == semester)
                .ToListAsync();

            var model = new GradeBookViewModel
            {
                ClassId = classId,
                SubjectId = subjectId,
                AcademicYear = academicYear,
                Semester = semester,
                ClassName = lop.ClassName ?? "N/A",
                SubjectName = mon.Name ?? "N/A",
                Students = students.Select(s =>
                {
                    var row = new StudentGradeRow
                    {
                        StudentId = s.Id,
                        StudentCode = s.StudentCode,
                        FullName = s.FullName
                    };

                    // Gán điểm từ DB
                    var studentGrades = allGrades.Where(g => g.StudentId == s.Id);
                    row.Mieng = studentGrades.FirstOrDefault(g => g.ExamType == "Miệng")?.Score;
                    row._15p = studentGrades.FirstOrDefault(g => g.ExamType == "15p")?.Score;
                    row.GiuaKy = studentGrades.FirstOrDefault(g => g.ExamType == "Giữa kỳ")?.Score;
                    row.CuoiKy = studentGrades.FirstOrDefault(g => g.ExamType == "Cuối kỳ")?.Score;

                    return row;
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGradeBook(GradeBookViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Có thể reload tên lớp/môn nếu cần
                return View("GradeBook", model);
            }

            var currentUserId = _userManager.GetUserId(User);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var student in model.Students)
                {
                    // Xử lý từng loại điểm, kèm theo AcademicYear và Semester
                    await SaveOrUpdateGrade(student.StudentId, model.SubjectId, model.AcademicYear, model.Semester, "Miệng", student.Mieng, currentUserId);
                    await SaveOrUpdateGrade(student.StudentId, model.SubjectId, model.AcademicYear, model.Semester, "15p", student._15p, currentUserId);
                    await SaveOrUpdateGrade(student.StudentId, model.SubjectId, model.AcademicYear, model.Semester, "Giữa kỳ", student.GiuaKy, currentUserId);
                    await SaveOrUpdateGrade(student.StudentId, model.SubjectId, model.AcademicYear, model.Semester, "Cuối kỳ", student.CuoiKy, currentUserId);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Đã lưu bảng điểm môn {model.SubjectName} lớp {model.ClassName} (HK{model.Semester} - {model.AcademicYear}) thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Lỗi lưu điểm: {ex.Message}");
                return View("GradeBook", model);
            }
        }

        private async Task SaveOrUpdateGrade(int studentId, int subjectId, string academicYear, int semester, string examType, double? score, string teacherId)
        {
            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.StudentId == studentId &&
                                          g.SubjectId == subjectId &&
                                          g.AcademicYear == academicYear &&
                                          g.Semester == semester &&
                                          g.ExamType == examType);

            if (score.HasValue)
            {
                if (grade != null)
                {
                    grade.Score = score.Value;
                    grade.TeacherId = teacherId;
                    grade.DateCreated = DateTime.Now;
                    _context.Grades.Update(grade);
                }
                else
                {
                    _context.Grades.Add(new Grade
                    {
                        StudentId = studentId,
                        SubjectId = subjectId,
                        AcademicYear = academicYear,
                        Semester = semester,
                        ExamType = examType,
                        Score = score.Value,
                        TeacherId = teacherId,
                        DateCreated = DateTime.Now
                    });
                }
            }
            else if (grade != null)
            {
                // Xóa nếu bỏ trống
                _context.Grades.Remove(grade);
            }
        }
    }
}