namespace backend.Models;

public class Dataset
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ViewName { get; set; } = string.Empty;

    public ICollection<DatasetField> Fields { get; set; } = new List<DatasetField>();
}
