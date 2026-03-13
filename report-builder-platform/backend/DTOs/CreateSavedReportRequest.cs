using System.Text.Json;

namespace backend.DTOs;

public class CreateSavedReportRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid DatasetId { get; set; }

    public JsonElement Definition { get; set; }
}
