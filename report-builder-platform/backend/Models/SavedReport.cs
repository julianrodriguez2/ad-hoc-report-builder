namespace backend.Models;

public class SavedReport
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DefinitionJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
}
