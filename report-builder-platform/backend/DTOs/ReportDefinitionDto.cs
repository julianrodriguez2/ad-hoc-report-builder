namespace backend.DTOs;

public class ReportDefinitionDto
{
    public Guid DatasetId { get; set; }

    public List<SelectedFieldDto> Fields { get; set; } = new();

    public List<FilterDefinitionDto> Filters { get; set; } = new();

    public List<GroupDefinitionDto> Grouping { get; set; } = new();

    public List<SummaryDefinitionDto> Summaries { get; set; } = new();
}
