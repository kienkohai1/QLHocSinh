using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLHocSinh.Models.ViewModels
{
    public class GradeEntryViewModel
    {
        [Required]
        public int ClassId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public string ExamType { get; set; }

        public string ClassName { get; set; }
        public string SubjectName { get; set; }

        // Danh sách học sinh để nhập điểm
        public List<StudentScoreItem> Students { get; set; } = new List<StudentScoreItem>();
    }

    public class StudentScoreItem
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }

        // Dùng nullable double (?) để biết học sinh nào chưa được nhập điểm
        [Range(0, 10, ErrorMessage = "Điểm phải từ 0 đến 10")]
        public double? Score { get; set; }
    }
}