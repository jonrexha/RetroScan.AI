namespace RetroScan.Api.Data;

public static class DatabaseClock
{
    // DATETIME(6) keeps microseconds; truncating here makes a response match what a later read returns.
    public static DateTime UtcNowForDatabase(this TimeProvider clock)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
