namespace backend.DTOs;

public class SavedReportSummaryDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid DatasetId { get; set; }

    public DateTime CreatedAt { get; set; }
}
