namespace backend.DTOs;

public class PreviewResultDto
{
    public List<PreviewColumnDto> Columns { get; set; } = new();

    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    public int RowCount { get; set; }

    public bool IsTruncated { get; set; }

    public string? DebugSql { get; set; }
}
