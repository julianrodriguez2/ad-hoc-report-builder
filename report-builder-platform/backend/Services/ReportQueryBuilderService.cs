using backend.DTOs;

namespace backend.Services;

public class ReportQueryBuilderService : IReportQueryBuilderService
{
    public string BuildPreviewQuery(ReportDefinitionDto definition)
    {
        return $"-- Preview query for dataset {definition.DatasetId}\nSELECT TOP 100 * FROM [PlaceholderView];";
    }

    public string BuildFullQuery(ReportDefinitionDto definition)
    {
        return $"-- Full query for dataset {definition.DatasetId}\nSELECT * FROM [PlaceholderView];";
    }
}
