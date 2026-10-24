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
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<EligibilityRule> EligibilityRules => Set<EligibilityRule>();
    public DbSet<RuleEvaluationResult> RuleEvaluationResults => Set<RuleEvaluationResult>();
    public DbSet<ApprovalWorkflowStep> ApprovalWorkflowSteps => Set<ApprovalWorkflowStep>();
    public DbSet<InformationRequest> InformationRequests => Set<InformationRequest>();
    public DbSet<InformationRequestResponse> InformationRequestResponses => Set<InformationRequestResponse>();
    public DbSet<FinanceHandoff> FinanceHandoffs => Set<FinanceHandoff>();
    public DbSet<LoanActivationRecord> LoanActivationRecords => Set<LoanActivationRecord>();
    public DbSet<SimulatedRepaymentInstallment> SimulatedRepaymentInstallments => Set<SimulatedRepaymentInstallment>();
    public DbSet<DocumentRequirement> DocumentRequirements => Set<DocumentRequirement>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ConsistencyFlag> ConsistencyFlags => Set<ConsistencyFlag>();
    public DbSet<ConsistencyFlagHistory> ConsistencyFlagHistories => Set<ConsistencyFlagHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    IQueryable<Department> IAppDbContext.Departments => Departments;
    IQueryable<College> IAppDbContext.Colleges => Colleges;
    IQueryable<Course> IAppDbContext.Courses => Courses;
    IQueryable<User> IAppDbContext.Users => Users;
    IQueryable<LoanApplication> IAppDbContext.LoanApplications => LoanApplications;
    IQueryable<ApplicationDocument> IAppDbContext.ApplicationDocuments => ApplicationDocuments;
    IQueryable<PasswordResetOtp> IAppDbContext.PasswordResetOtps => PasswordResetOtps;
    IQueryable<EligibilityRule> IAppDbContext.EligibilityRules => EligibilityRules;
    IQueryable<RuleEvaluationResult> IAppDbContext.RuleEvaluationResults => RuleEvaluationResults;
    IQueryable<ApprovalWorkflowStep> IAppDbContext.ApprovalWorkflowSteps => ApprovalWorkflowSteps;
    IQueryable<InformationRequest> IAppDbContext.InformationRequests => InformationRequests;
    IQueryable<InformationRequestResponse> IAppDbContext.InformationRequestResponses => InformationRequestResponses;
    IQueryable<FinanceHandoff> IAppDbContext.FinanceHandoffs => FinanceHandoffs;
    IQueryable<DocumentRequirement> IAppDbContext.DocumentRequirements => DocumentRequirements;
    IQueryable<Notification> IAppDbContext.Notifications => Notifications;
    IQueryable<ConsistencyFlag> IAppDbContext.ConsistencyFlags => ConsistencyFlags;
    IQueryable<ConsistencyFlagHistory> IAppDbContext.ConsistencyFlagHistories => ConsistencyFlagHistories;

    public void AddDepartment(Department department) => Departments.Add(department);
    public void AddCollege(College college) => Colleges.Add(college);
    public void AddCourse(Course course) => Courses.Add(course);
    public void AddLoanApplication(LoanApplication application) => LoanApplications.Add(application);
    public void AddApplicationDocument(ApplicationDocument document) => ApplicationDocuments.Add(document);
    public void AddPasswordResetOtp(PasswordResetOtp otp) => PasswordResetOtps.Add(otp);
    public void AddEligibilityRule(EligibilityRule rule) => EligibilityRules.Add(rule);
    public void AddRuleEvaluationResults(IEnumerable<RuleEvaluationResult> results) => RuleEvaluationResults.AddRange(results);
    public void AddApprovalWorkflowStep(ApprovalWorkflowStep step) => ApprovalWorkflowSteps.Add(step);
    public void AddInformationRequest(InformationRequest request) => InformationRequests.Add(request);
    public void AddInformationRequestResponse(InformationRequestResponse response) => InformationRequestResponses.Add(response);
    public void AddFinanceHandoff(FinanceHandoff handoff) => FinanceHandoffs.Add(handoff);
    public void AddNotification(Notification notification) => Notifications.Add(notification);
    public void AddConsistencyFlag(ConsistencyFlag flag) => ConsistencyFlags.Add(flag);
    public void AddConsistencyFlagHistory(ConsistencyFlagHistory history) => ConsistencyFlagHistories.Add(history);

    public void RemoveDepartment(Department department) => Departments.Remove(department);
    public void RemoveCollege(College college) => Colleges.Remove(college);
    public void RemoveCourse(Course course) => Courses.Remove(course);
    public void RemoveApplicationDocument(ApplicationDocument document) => ApplicationDocuments.Remove(document);
    public void RemoveEligibilityRule(EligibilityRule rule) => EligibilityRules.Remove(rule);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActorRole).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(120).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(100);
            entity.Property(x => x.Outcome).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Metadata).HasMaxLength(1000);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.ActorUserId, x.CreatedAtUtc });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notification");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Link).HasMaxLength(300);
            entity.HasIndex(x => new { x.UserId, x.ReadAtUtc, x.CreatedAtUtc });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

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
            entity.Property(u => u.EmploymentType).HasMaxLength(50);
            entity.HasOne(u => u.Department).WithMany().HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Department"); entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).HasMaxLength(100).IsRequired();
        });
        modelBuilder.Entity<College>(entity =>
        {
            entity.ToTable("College"); entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.City).HasMaxLength(100).IsRequired();
        });
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Course"); entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(150).IsRequired();
            entity.Property(c => c.Level).HasMaxLength(30).IsRequired();
        });
        modelBuilder.Entity<LoanApplication>(entity =>
        {
            entity.ToTable("LoanApplication"); entity.HasKey(a => a.Id);
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
            entity.ToTable("ApplicationDocument"); entity.HasKey(d => d.Id);
            entity.Property(d => d.DocumentType).HasMaxLength(100).IsRequired();
            entity.Property(d => d.FileName).HasMaxLength(255).IsRequired();
            entity.Property(d => d.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(d => d.VerificationStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(d => d.VerificationComments).HasMaxLength(1000);
            entity.HasOne(d => d.Application).WithMany(a => a.Documents).HasForeignKey(d => d.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PasswordResetOtp>(entity =>
        {
            entity.ToTable("PasswordResetOtp");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.OtpHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ResetTokenHash).HasMaxLength(64);
            entity.HasIndex(x => new { x.Email, x.CreatedAtUtc });
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<EligibilityRule>(entity =>
        {
            entity.ToTable("EligibilityRule"); entity.HasKey(r => r.Id);
            entity.Property(r => r.RuleName).HasMaxLength(150).IsRequired();
            entity.Property(r => r.RuleCategory).HasMaxLength(50).IsRequired();
            entity.Property(r => r.ConditionField).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Operator).HasMaxLength(5).IsRequired();
            entity.Property(r => r.ConditionValue).HasColumnType("decimal(12,2)");
            entity.Property(r => r.ErrorMessage).HasMaxLength(300).IsRequired();
            entity.Property(r => r.Severity).HasConversion<string>().HasMaxLength(20);
        });
        modelBuilder.Entity<RuleEvaluationResult>(entity =>
        {
            entity.ToTable("RuleEvaluationResult"); entity.HasKey(r => r.Id);
            entity.Property(r => r.RuleName).HasMaxLength(150).IsRequired();
            entity.Property(r => r.RuleCategory).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Severity).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.EvaluatedValue).HasMaxLength(50).IsRequired();
            entity.Property(r => r.FailureMessage).HasMaxLength(300);
            entity.HasOne(r => r.Application).WithMany().HasForeignKey(r => r.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<InformationRequest>(entity =>
        {
            entity.ToTable("InformationRequest"); entity.HasKey(x => x.Id);
            entity.Property(x => x.RequestedByName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Question).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.RequiredDocumentType).HasMaxLength(100);
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Instructions).HasMaxLength(1000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<InformationRequestResponse>(entity =>
        {
            entity.ToTable("InformationRequestResponse"); entity.HasKey(x => x.Id);
            entity.Property(x => x.RespondedByName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.ResponseText).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.DocumentFileName).HasMaxLength(255);
            entity.Property(x => x.DocumentStoredFileName).HasMaxLength(255);
            entity.HasOne(x => x.InformationRequest).WithMany(x => x.Responses).HasForeignKey(x => x.InformationRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ApplicationDocument).WithMany().HasForeignKey(x => x.ApplicationDocumentId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<EmployeeDocument>(entity =>
        {
            entity.ToTable("EmployeeDocument"); entity.HasKey(x => x.Id);
            entity.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.EmployeeId, x.UploadedAtUtc });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DocumentRequirement>(entity =>
        {
            entity.ToTable("DocumentRequirement"); entity.HasKey(x => x.Id);
            entity.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EmploymentType).HasMaxLength(50);
            entity.Property(x => x.EducationLevel).HasMaxLength(50);
            entity.Property(x => x.MinimumLoanAmount).HasColumnType("decimal(12,2)");
            entity.Property(x => x.MaximumLoanAmount).HasColumnType("decimal(12,2)");
            entity.HasIndex(x => new { x.IsActive, x.DisplayOrder });
        });
        modelBuilder.Entity<FinanceHandoff>(entity =>
        {
            entity.ToTable("FinanceHandoff"); entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ApplicationId).IsUnique();
            entity.Property(x => x.BankReferenceNumber).HasMaxLength(100);
            entity.Property(x => x.ConfirmationReference).HasMaxLength(100);
            entity.Property(x => x.ProcessedAmount).HasColumnType("decimal(12,2)");
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(x => x.FinanceComments).HasMaxLength(1000);
            entity.Property(x => x.FailureReason).HasMaxLength(500);
            entity.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<LoanActivationRecord>(entity =>
        {
            entity.ToTable("LoanActivationRecord"); entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ApplicationId).IsUnique();
            entity.Property(x => x.PrincipalAmount).HasColumnType("decimal(12,2)");
            entity.Property(x => x.AnnualInterestRatePercent).HasColumnType("decimal(6,3)");
            entity.Property(x => x.ActivationReference).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SimulatedRepaymentInstallment>(entity =>
        {
            entity.ToTable("SimulatedRepaymentInstallment"); entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ActivationRecordId, x.InstallmentNumber }).IsUnique();
            entity.Property(x => x.ScheduledAmount).HasColumnType("decimal(12,2)");
            entity.Property(x => x.SimulatedAmountPaid).HasColumnType("decimal(12,2)");
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.SimulationNote).HasMaxLength(500);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne(x => x.ActivationRecord).WithMany(x => x.Installments).HasForeignKey(x => x.ActivationRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApprovalWorkflowStep>(entity =>
        {
            entity.ToTable("ApprovalWorkflowStep"); entity.HasKey(w => w.Id);
            entity.Property(w => w.ActorName).HasMaxLength(150).IsRequired();
            entity.Property(w => w.ActorRole).HasMaxLength(20).IsRequired();
            entity.Property(w => w.Action).HasConversion<string>().HasMaxLength(30);
            entity.Property(w => w.Comments).HasMaxLength(500);
            entity.HasOne(w => w.Application).WithMany().HasForeignKey(w => w.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ConsistencyFlag>(entity =>
        {
            entity.ToTable("ConsistencyFlag");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RuleCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Severity).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.ResolutionNotes).HasMaxLength(1000);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.ApplicationId, x.RuleCode }).HasDatabaseName("IX_ConsistencyFlag_ApplicationId_RuleCode_Active").HasFilter("[Status] IN ('Open','UnderInvestigation')").IsUnique();
            entity.HasIndex(x => new { x.Status, x.DetectedAtUtc });
        });
        modelBuilder.Entity<ConsistencyFlagHistory>(entity =>
        {
            entity.ToTable("ConsistencyFlagHistory");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne(x => x.ConsistencyFlag).WithMany(x => x.History).HasForeignKey(x => x.ConsistencyFlagId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.ConsistencyFlagId, x.CreatedAtUtc });
        });
    }
}
