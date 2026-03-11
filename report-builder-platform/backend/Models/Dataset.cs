namespace backend.Models;

public class Dataset
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ViewName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<DatasetField> Fields { get; set; } = new List<DatasetField>();
}
