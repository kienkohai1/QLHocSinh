using System.Diagnostics;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } // Toán, Lý, Hóa...
    public int Credits { get; set; } // Số tín chỉ hoặc số tiết/tuần

    public ICollection<Grade> Grades { get; set; }
}