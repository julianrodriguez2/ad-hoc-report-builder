using backend.DTOs;

namespace backend.Services;

public interface IReportExportService
{
    Task<byte[]> ExportPdfAsync(ReportDefinitionDto definition, CancellationToken cancellationToken = default);

    Task<byte[]> ExportExcelAsync(ReportDefinitionDto definition, CancellationToken cancellationToken = default);
}
