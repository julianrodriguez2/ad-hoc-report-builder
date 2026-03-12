namespace backend.Services;

public sealed class GuardrailExecutionSettings
{
    public int PreviewRowLimit { get; init; }

    public int MaxExecutionRowLimit { get; init; }

    public int TimeoutSeconds { get; init; }
}
