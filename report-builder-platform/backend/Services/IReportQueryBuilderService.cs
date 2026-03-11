using backend.DTOs;
using backend.Models;

namespace backend.Services;

public interface IReportQueryBuilderService
{
    Task<QueryBuildResult> BuildPreviewQueryAsync(ReportDefinitionDto definition, CancellationToken cancellationToken = default);

    Task<Dataset> GetValidatedDatasetAsync(Guid datasetId, CancellationToken cancellationToken = default);

    Task<Dictionary<string, DatasetField>> GetDatasetFieldMapAsync(Guid datasetId, CancellationToken cancellationToken = default);
}
