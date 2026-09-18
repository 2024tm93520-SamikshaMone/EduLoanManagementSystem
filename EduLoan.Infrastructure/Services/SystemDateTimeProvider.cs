using EduLoan.Application.Interfaces;

namespace EduLoan.Infrastructure.Services;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
