namespace Ropoly.Core.Match
{
    public enum CreatureSelectionResult
    {
        Success = 0,
        InvalidPlayerIndex = 1,
        UnknownCreature = 2,
        CreatureAlreadySelected = 3,
        MatchAlreadyStarted = 4,
    }
}
