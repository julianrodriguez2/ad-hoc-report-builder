using backend.DTOs;

namespace backend.Services;

public interface IReportQueryBuilderService
{
    string BuildPreviewQuery(ReportDefinitionDto definition);

    string BuildFullQuery(ReportDefinitionDto definition);
}
