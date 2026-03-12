namespace backend.Services;

public class ReportingOptions
{
    public int DefaultPreviewRowLimit { get; set; } = 100;

    public int DefaultMaxExecutionRowLimit { get; set; } = 10000;

    public int DefaultTimeoutSeconds { get; set; } = 10;
}
