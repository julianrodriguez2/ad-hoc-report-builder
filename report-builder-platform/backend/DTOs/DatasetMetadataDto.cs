namespace backend.DTOs;

public class DatasetMetadataDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? PreviewRowLimit { get; set; }

    public int? MaxExecutionRowLimit { get; set; }

    public bool RequireAtLeastOneFilter { get; set; }

    public bool RequireDateFilter { get; set; }

    public int? LargeDatasetThreshold { get; set; }

    public int? TimeoutSeconds { get; set; }
}
