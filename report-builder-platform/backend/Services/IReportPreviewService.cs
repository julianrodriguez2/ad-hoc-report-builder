using backend.DTOs;

namespace backend.Services;

public interface IReportPreviewService
{
    Task<PreviewResultDto> ExecutePreviewAsync(ReportDefinitionDto definition, CancellationToken cancellationToken = default);
}
