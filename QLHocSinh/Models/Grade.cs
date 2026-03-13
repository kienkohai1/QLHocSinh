public class Grade
{
    public int Id { get; set; }
    public double Score { get; set; }

    // Loại điểm: Miệng, 15p, Giữa kỳ, Cuối kỳ
    public string ExamType { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.Now;

    public int StudentId { get; set; }
    public Student Student { get; set; }

    public int SubjectId { get; set; }
    public Subject Subject { get; set; }

    // Lưu vết giáo viên nào đã nhập điểm này
    public string TeacherId { get; set; }
    public ApplicationUser Teacher { get; set; }
}