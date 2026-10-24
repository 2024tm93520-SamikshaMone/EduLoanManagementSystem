namespace EduLoan.Domain.Entities;

public static class ConsistencyFlagTransitionPolicy
{
    public static bool CanTransition(ConsistencyFlagStatus current, ConsistencyFlagStatus next) => current switch
    {
        ConsistencyFlagStatus.Open => next is ConsistencyFlagStatus.UnderInvestigation or ConsistencyFlagStatus.Resolved or ConsistencyFlagStatus.Dismissed,
        ConsistencyFlagStatus.UnderInvestigation => next is ConsistencyFlagStatus.Resolved or ConsistencyFlagStatus.Dismissed,
        _ => false
    };

    public static bool RequiresNotes(ConsistencyFlagStatus next) => next is ConsistencyFlagStatus.Resolved or ConsistencyFlagStatus.Dismissed;
}
