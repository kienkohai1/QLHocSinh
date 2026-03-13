using System.Diagnostics;

public class Student
{
    public int Id { get; set; }
    public string StudentCode { get; set; } // Mã học sinh (ví dụ: HS202401)
    public string FullName { get; set; }
    public DateTime DateOfBirth { get; set; }

    // Khóa ngoại đến Lớp học
    public int ClassId { get; set; }
    public Class Class { get; set; }

    // Liên kết với tài khoản Phụ huynh (ApplicationUser)
    public string ParentId { get; set; }
    public ApplicationUser Parent { get; set; }

    public ICollection<Grade> Grades { get; set; }
}