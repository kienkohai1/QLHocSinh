using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLHocSinh.Models;
using System.Threading.Tasks;

namespace QLHocSinh.Controllers
{
    [Authorize(Roles = "Admin")]

    public class ClassesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Xem danh sách lớp học
        public async Task<IActionResult> Index()
        {
            var classes = await _context.Classes.ToListAsync();
            return View(classes);
        }

        // 2. GET: Thêm lớp học mới
        public IActionResult Create()
        {
            return View();
        }

        // 3. POST: Thêm lớp học mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Sử dụng @class vì class là từ khóa, hoặc bạn có thể dùng tên biến khác như classObj, model
        public async Task<IActionResult> Create([Bind("Id,ClassName,SchoolYear")] Class classObj)
        {
            if (ModelState.IsValid)
            {
                _context.Add(classObj);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(classObj);
        }

        // 4. GET: Sửa lớp học
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var classObj = await _context.Classes.FindAsync(id);
            if (classObj == null) return NotFound();

            return View(classObj);
        }

        // 5. POST: Sửa lớp học
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClassName,SchoolYear")] Class classObj)
        {
            if (id != classObj.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(classObj);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassExists(classObj.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(classObj);
        }

        // 6. Xóa lớp học
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var classObj = await _context.Classes.FirstOrDefaultAsync(m => m.Id == id);
            if (classObj == null) return NotFound();

            return View(classObj);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classObj = await _context.Classes.FindAsync(id);
            if (classObj != null)
            {
                _context.Classes.Remove(classObj);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var classObj = await _context.Classes
                .Include(c => c.Students)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classObj == null) return NotFound();

            return View(classObj);
        }

        private bool ClassExists(int id)
        {
            return _context.Classes.Any(e => e.Id == id);
        }
    }
}