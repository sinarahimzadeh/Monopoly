using System;

namespace Ropoly.Core.Match
{
    [Serializable]
    public sealed class DiceRoll
    {
        public DiceRoll(int firstDie, int secondDie)
        {
            if (!IsDieValueValid(firstDie))
            {
                throw new ArgumentOutOfRangeException(nameof(firstDie));
            }

            if (!IsDieValueValid(secondDie))
            {
                throw new ArgumentOutOfRangeException(nameof(secondDie));
            }

            FirstDie = firstDie;
            SecondDie = secondDie;
        }

        public int FirstDie { get; }
        public int SecondDie { get; }
        public int Total => FirstDie + SecondDie;
        public bool IsDouble => FirstDie == SecondDie;

        public static bool IsDieValueValid(int value)
        {
            return value >= 1 && value <= 6;
        }
    }
}
