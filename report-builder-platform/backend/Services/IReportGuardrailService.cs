using backend.DTOs;
using backend.Models;

namespace backend.Services;

public interface IReportGuardrailService
{
    GuardrailExecutionSettings ValidatePreviewRequest(
        ReportDefinitionDto definition,
        Dataset dataset,
        IReadOnlyDictionary<string, DatasetField> datasetFieldMap);

    GuardrailExecutionSettings ValidateExecutionRequest(
        ReportDefinitionDto definition,
        Dataset dataset,
        IReadOnlyDictionary<string, DatasetField> datasetFieldMap);

    bool HasAnyFilters(ReportDefinitionDto definition);

    bool HasDateFilter(ReportDefinitionDto definition, IReadOnlyDictionary<string, DatasetField> datasetFieldMap);

    int ResolvePreviewRowLimit(Dataset dataset);

    int ResolveExecutionRowLimit(Dataset dataset);

    int ResolveTimeoutSeconds(Dataset dataset);
}
