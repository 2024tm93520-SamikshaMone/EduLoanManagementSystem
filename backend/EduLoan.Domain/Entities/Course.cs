namespace EduLoan.Domain.Entities;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty; // UG / PG / Diploma / Certification
    public int StandardDurationMonths { get; set; }
    public bool IsActive { get; set; } = true;
}
