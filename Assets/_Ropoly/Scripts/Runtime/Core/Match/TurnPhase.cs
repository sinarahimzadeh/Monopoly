namespace Ropoly.Core.Match
{
    public enum TurnPhase
    {
        Inactive = 0,
        AwaitingRoll = 1,
        AwaitingMovement = 2,
        Moving = 3,
        AwaitingTurnEnd = 4,
        AwaitingPropertyDecision = 5,
    }
}
