public class Class
{
    public int Id { get; set; }
    public string ClassName { get; set; } // Ví dụ: 10A1, 11B2
    public string SchoolYear { get; set; } // Ví dụ: 2023-2024

    public ICollection<Student> Students { get; set; }
    public ICollection<Assignment> Assignments { get; set; }
}