using EduLoan.Domain.Entities;
using Xunit;

namespace EduLoan.Application.Tests.Features.Admin;

public sealed class ConsistencyFlagTransitionPolicyTests
{
    [Theory]
    [InlineData(ConsistencyFlagStatus.Open, ConsistencyFlagStatus.UnderInvestigation, true)]
    [InlineData(ConsistencyFlagStatus.Open, ConsistencyFlagStatus.Resolved, true)]
    [InlineData(ConsistencyFlagStatus.Open, ConsistencyFlagStatus.Dismissed, true)]
    [InlineData(ConsistencyFlagStatus.UnderInvestigation, ConsistencyFlagStatus.Resolved, true)]
    [InlineData(ConsistencyFlagStatus.UnderInvestigation, ConsistencyFlagStatus.Dismissed, true)]
    [InlineData(ConsistencyFlagStatus.UnderInvestigation, ConsistencyFlagStatus.Open, false)]
    [InlineData(ConsistencyFlagStatus.Resolved, ConsistencyFlagStatus.Open, false)]
    [InlineData(ConsistencyFlagStatus.Dismissed, ConsistencyFlagStatus.UnderInvestigation, false)]
    public void CanTransition_OnlyAllowsSupportedForwardTransitions(ConsistencyFlagStatus current, ConsistencyFlagStatus next, bool expected)
        => Assert.Equal(expected, ConsistencyFlagTransitionPolicy.CanTransition(current, next));

    [Theory]
    [InlineData(ConsistencyFlagStatus.Resolved, true)]
    [InlineData(ConsistencyFlagStatus.Dismissed, true)]
    [InlineData(ConsistencyFlagStatus.Open, false)]
    [InlineData(ConsistencyFlagStatus.UnderInvestigation, false)]
    public void RequiresNotes_OnlyForTerminalReviewActions(ConsistencyFlagStatus next, bool expected)
        => Assert.Equal(expected, ConsistencyFlagTransitionPolicy.RequiresNotes(next));
}
