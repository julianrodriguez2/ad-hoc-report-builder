namespace backend.Services;

public class QueryBuildResult
{
    public string Sql { get; set; } = string.Empty;

    public Dictionary<string, object?> Parameters { get; set; } = new();
}
