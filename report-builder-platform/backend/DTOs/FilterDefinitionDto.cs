namespace backend.DTOs;

public class FilterDefinitionDto
{
    public string? Id { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? DataType { get; set; }

    public string Operator { get; set; } = string.Empty;

    public object? Value { get; set; }
}
