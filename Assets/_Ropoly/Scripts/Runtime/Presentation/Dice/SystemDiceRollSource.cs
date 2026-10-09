using System;
using Ropoly.Core.Match;

namespace Ropoly.Presentation.Dice
{
    public sealed class SystemDiceRollSource : IDiceRollSource
    {
        private readonly Random _random;

        public SystemDiceRollSource()
            : this(Environment.TickCount)
        {
        }

        public SystemDiceRollSource(int seed)
        {
            _random = new Random(seed);
        }

        public int NextDieValue()
        {
            return _random.Next(1, 7);
        }
    }
}
