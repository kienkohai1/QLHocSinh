using System.Diagnostics;

public class Student
{
    public int Id { get; set; }
    public string StudentCode { get; set; }
    public string FullName { get; set; }
    public DateTime DateOfBirth { get; set; }

    // Khóa ngoại đến Lớp học
    public int ClassId { get; set; }
    public Class? Class { get; set; } // Thêm dấu ?

    public string? ParentId { get; set; } // Thêm dấu ?
    public ApplicationUser? Parent { get; set; }

    public ICollection<Grade> Grades { get; set; }
}