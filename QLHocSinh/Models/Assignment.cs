public class Assignment
{
    public int Id { get; set; }

    public string TeacherId { get; set; }
    public ApplicationUser Teacher { get; set; }

    public int SubjectId { get; set; }
    public Subject Subject { get; set; }

    public int ClassId { get; set; }
    public Class Class { get; set; }
}