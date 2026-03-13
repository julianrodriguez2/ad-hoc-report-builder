using System.Text.Json;

namespace backend.DTOs;

public class UpdateSavedReportRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public JsonElement Definition { get; set; }
}
