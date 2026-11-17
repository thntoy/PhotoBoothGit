using System;

/// <summary>Fixed output-screen timeout, independent of pointer activity and earlier screens.</summary>
public sealed class BoothOutputTimer
{
    public const int DurationSeconds = 120;
    private double deadline;
    public bool IsRunning { get; private set; }

    public void StartIfNeeded(double now)
    {
        if (IsRunning) return;
        deadline = now + DurationSeconds;
        IsRunning = true;
    }

    public int RemainingSeconds(double now)
    {
        if (!IsRunning) return DurationSeconds;
        return Math.Max(0, (int)Math.Ceiling(deadline - now));
    }

    public bool HasExpired(double now)
    {
        return IsRunning && now >= deadline;
    }

    public void Reset()
    {
        IsRunning = false;
        deadline = 0;
    }
}
