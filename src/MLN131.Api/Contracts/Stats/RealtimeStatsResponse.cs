namespace MLN131.Api.Contracts.Stats;

public sealed class RealtimeStatsResponse
{
    public DateTimeOffset AsOf { get; set; }

    public int VisitorsOnline { get; set; }
    public int LoggedInOnline { get; set; }

    public int DistinctUsersAnsweredTotal { get; set; }
    public int DistinctUsersAnsweredLast24h { get; set; }

    public double AvgSessionDurationSecondsLast24h { get; set; }
}

