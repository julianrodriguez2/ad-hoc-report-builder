namespace backend.DTOs;

public class SummaryDefinitionDto
{
    public string FieldName { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public string? Alias { get; set; }
}
