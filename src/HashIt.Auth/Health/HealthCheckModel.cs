using Microsoft.Extensions.Diagnostics.HealthChecks;
/// <summary>
/// Represents a health check result.
/// </summary>
public class HealthCheckModel
{
    /// <summary>
    /// Creates a new instance of the <see cref="HealthCheckModel"/> class.
    /// </summary>
    /// <param name="healthReport">HealthReport.</param>
    public HealthCheckModel(HealthReport healthReport)
    {
        Status = healthReport.Status.ToString();
        TotalDuration = (int)healthReport.TotalDuration.TotalMilliseconds;
        Results = healthReport.Entries.ToDictionary(x => x.Key, x => new HealthCheckEntryModel(x.Value));
    }

    /// <summary>
    /// Overall health check status.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Total duration of all checks in milliseconds.
    /// </summary>
    public int TotalDuration { get; set; }

    /// <summary>
    /// Health check entry results.
    /// </summary>
    public Dictionary<string, HealthCheckEntryModel> Results { get; set; }
}

/// <summary>
/// Represents an health check entry.
/// </summary>
public class HealthCheckEntryModel
{
    /// <summary>
    /// Creates a new instance of the <see cref="HealthCheckEntryModel"/> class.
    /// </summary>
    /// <param name="healthReportEntry">HealthReportEntry.</param>
    public HealthCheckEntryModel(HealthReportEntry healthReportEntry)
    {
        Duration = (int)healthReportEntry.Duration.TotalMilliseconds;
        Data = healthReportEntry.Data.ToDictionary(x => x.Key, x => x.Value);
        Status = healthReportEntry.Status.ToString();
        Description = healthReportEntry.Description;
        Tags = healthReportEntry.Tags.ToList();
    }

    /// <summary>
    /// Health check entry status.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Duration of the check in milliseconds.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Description of the check.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Additional data associated with the check.
    /// </summary>
    public Dictionary<string, object> Data { get; set; }

    /// <summary>
    /// Additional tags associated with the check.
    /// </summary>
    public List<string> Tags { get; set; }
}
