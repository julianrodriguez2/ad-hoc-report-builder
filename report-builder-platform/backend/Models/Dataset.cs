namespace backend.Models;

public class Dataset
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ViewName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int? PreviewRowLimit { get; set; }

    public int? MaxExecutionRowLimit { get; set; }

    public bool RequireAtLeastOneFilter { get; set; }

    public bool RequireDateFilter { get; set; }

    public int? LargeDatasetThreshold { get; set; }

    public int? TimeoutSeconds { get; set; }

    public ICollection<DatasetField> Fields { get; set; } = new List<DatasetField>();
}
