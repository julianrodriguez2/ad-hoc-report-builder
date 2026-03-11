namespace backend.DTOs;

public class FilterDefinitionDto
{
    public string FieldName { get; set; } = string.Empty;

    public string Operator { get; set; } = string.Empty;

    public object? Value { get; set; }
}
