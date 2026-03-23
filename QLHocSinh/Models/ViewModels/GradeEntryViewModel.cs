using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class GradeBookViewModel
{
    public int ClassId { get; set; }
    public int SubjectId { get; set; }

    // Thêm trường để bind từ view lên
    public string AcademicYear { get; set; } = "";
    public int Semester { get; set; }

    public string ClassName { get; set; } = "";
    public string SubjectName { get; set; } = "";

    public List<StudentGradeRow> Students { get; set; } = new();

    // Để tính trung bình nếu cần
    public bool ShowAverage { get; set; } = true;
}

public class StudentGradeRow
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = "";
    public string FullName { get; set; } = "";

    public double? Mieng { get; set; }
    public double? _15p { get; set; }
    public double? GiuaKy { get; set; }
    public double? CuoiKy { get; set; }

    public double? TrungBinh =>
        (Mieng + _15p + GiuaKy * 2 + CuoiKy * 3) / 7;
}