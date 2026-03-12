namespace backend.DTOs;

public class SummaryDefinitionDto
{
    public string FieldName { get; set; } = string.Empty;

    public string Aggregation { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;

    public int SummaryOrder { get; set; }
}
