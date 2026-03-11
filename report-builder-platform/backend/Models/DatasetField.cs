namespace backend.Models;

public class DatasetField
{
    public int Id { get; set; }

    public int DatasetId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public bool IsFilterable { get; set; }

    public bool IsGroupable { get; set; }

    public bool IsSummarizable { get; set; }

    public Dataset? Dataset { get; set; }
}
