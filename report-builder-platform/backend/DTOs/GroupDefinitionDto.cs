namespace backend.DTOs;

public class GroupDefinitionDto
{
    public string FieldName { get; set; } = string.Empty;

    public string SortDirection { get; set; } = "asc";

    public int GroupOrder { get; set; }
}
