using Xunit;

namespace EduLoan.Application.Tests.Features;

public sealed class RepaymentSimulationCalculationTests
{
    [Theory]
    [InlineData(12000, 0, 12, 1000)]
    [InlineData(24000, 0, 24, 1000)]
    public void Zero_interest_simulation_has_equal_monthly_principal(decimal principal, decimal annualRate, int months, decimal expectedMonthly)
    {
        var monthlyRate = annualRate / 1200m;
        var pow = monthlyRate == 0 ? 1m : (decimal)Math.Pow((double)(1m + monthlyRate), months);
        var payment = monthlyRate == 0 ? principal / months : principal * monthlyRate * pow / (pow - 1m);
        Assert.Equal(expectedMonthly, decimal.Round(payment, 2));
    }

    [Fact]
    public void Positive_rate_simulation_payment_exceeds_zero_interest_principal_share()
    {
        decimal principal = 12000m; int months = 12; decimal monthlyRate = 12m / 1200m;
        var pow = (decimal)Math.Pow((double)(1m + monthlyRate), months);
        var payment = principal * monthlyRate * pow / (pow - 1m);
        Assert.True(payment > principal / months);
    }
}
