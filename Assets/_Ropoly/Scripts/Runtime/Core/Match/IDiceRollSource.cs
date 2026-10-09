namespace Ropoly.Core.Match
{
    /// <summary>
    /// Supplies authoritative die values. The local implementation uses System.Random;
    /// a future online host can replace it without changing the match rules.
    /// </summary>
    public interface IDiceRollSource
    {
        int NextDieValue();
    }
}
