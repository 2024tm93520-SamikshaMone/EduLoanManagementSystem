using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<College> Colleges => Set<College>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();

    // IAppDbContext explicit surface — IQueryable views + mutation helpers,
    // so Application-layer handlers depend on this narrow interface, not EF Core directly.
    IQueryable<Department> IAppDbContext.Departments => Departments;
    IQueryable<College> IAppDbContext.Colleges => Colleges;
    IQueryable<Course> IAppDbContext.Courses => Courses;
    IQueryable<User> IAppDbContext.Users => Users;
    IQueryable<LoanApplication> IAppDbContext.LoanApplications => LoanApplications;
    IQueryable<ApplicationDocument> IAppDbContext.ApplicationDocuments => ApplicationDocuments;

    public void AddDepartment(Department department) => Departments.Add(department);
    public void AddCollege(College college) => Colleges.Add(college);
    public void AddCourse(Course course) => Courses.Add(course);
    public void AddLoanApplication(LoanApplication application) => LoanApplications.Add(application);
    public void AddApplicationDocument(ApplicationDocument document) => ApplicationDocuments.Add(document);

    public void RemoveDepartment(Department department) => Departments.Remove(department);
    public void RemoveCollege(College college) => Colleges.Remove(college);
    public void RemoveCourse(Course course) => Courses.Remove(course);
    public void RemoveApplicationDocument(ApplicationDocument document) => ApplicationDocuments.Remove(document);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.EmployeeCode).HasMaxLength(20).IsRequired();
            entity.HasIndex(u => u.EmployeeCode).IsUnique();
            entity.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(u => u.MonthlySalary).HasColumnType("decimal(12,2)");
            entity.HasOne(u => u.Department)
                  .WithMany()
                  .HasForeignKey(u => u.DepartmentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Department");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<College>(entity =>
        {
            entity.ToTable("College");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.City).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Course");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(150).IsRequired();
            entity.Property(c => c.Level).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<LoanApplication>(entity =>
        {
            entity.ToTable("LoanApplication");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.ApplicationNumber).HasMaxLength(20).IsRequired();
            entity.HasIndex(a => a.ApplicationNumber).IsUnique();
            entity.Property(a => a.Specialization).HasMaxLength(150);
            entity.Property(a => a.TotalEducationFees).HasColumnType("decimal(12,2)");
            entity.Property(a => a.RequestedAmount).HasColumnType("decimal(12,2)");
            entity.Property(a => a.EducationPurpose).HasMaxLength(500);
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(a => a.College).WithMany().HasForeignKey(a => a.CollegeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(a => a.Course).WithMany().HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApplicationDocument>(entity =>
        {
            entity.ToTable("ApplicationDocument");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.DocumentType).HasMaxLength(100).IsRequired();
            entity.Property(d => d.FileName).HasMaxLength(255).IsRequired();
            entity.Property(d => d.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(d => d.ContentType).HasMaxLength(100).IsRequired();

            entity.HasOne(d => d.Application)
                  .WithMany(a => a.Documents)
                  .HasForeignKey(d => d.ApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
