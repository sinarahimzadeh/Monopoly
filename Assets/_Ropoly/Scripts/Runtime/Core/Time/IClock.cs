using System;

namespace Ropoly.Core.Time
{
    /// <summary>
    /// Pure C# clock abstraction allowing deterministic time in tests and authoritative simulation.
    /// </summary>
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public static SystemClock Instance { get; } = new SystemClock();

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    public sealed class TestClock : IClock
    {
        public TestClock(DateTimeOffset initialTime)
        {
            UtcNow = initialTime;
        }

        public DateTimeOffset UtcNow { get; set; }

        public void Advance(TimeSpan duration)
        {
            UtcNow = UtcNow.Add(duration);
        }

        public void AdvanceSeconds(double seconds)
        {
            UtcNow = UtcNow.AddSeconds(seconds);
        }
    }
}
