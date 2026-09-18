namespace EduLoan.Application.Interfaces;

public interface IDateTimeProvider
{
    DateOnly Today { get; }
}
