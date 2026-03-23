using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLHocSinh.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace QLHocSinh.Controllers
{
    // Bắt buộc người dùng phải đăng nhập mới được xem
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Chức năng Index hiển thị trang hồ sơ
        public async Task<IActionResult> Index()
        {
            // 1. Lấy ID của tài khoản (Phụ huynh hoặc Học sinh) đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Tìm dữ liệu Học sinh khớp với tài khoản này
            // Giả sử tài khoản đăng nhập được lưu ở trường ParentId
            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Subject) // Lấy luôn tên môn học
                .FirstOrDefaultAsync(s => s.ParentId == userId);

            // 3. Xử lý trường hợp chưa có dữ liệu liên kết
            if (student == null)
            {
                return NotFound("Tài khoản của bạn chưa được liên kết với hồ sơ học sinh nào.");
            }

            // 4. Trả dữ liệu về cho View hiển thị
            return View(student);
        }
    }
}