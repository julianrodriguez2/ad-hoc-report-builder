using backend.DTOs;
using backend.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/datasets")]
public class DatasetController(IDatasetRepository datasetRepository) : ControllerBase
{
    private readonly IDatasetRepository _datasetRepository = datasetRepository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DatasetMetadataDto>>> GetDatasets(CancellationToken cancellationToken)
    {
        var datasets = await _datasetRepository.GetDatasetsAsync(cancellationToken);

        var response = datasets.Select(dataset => new DatasetMetadataDto
        {
            Id = dataset.Id,
            Name = dataset.Name,
            Description = dataset.Description,
            PreviewRowLimit = dataset.PreviewRowLimit,
            MaxExecutionRowLimit = dataset.MaxExecutionRowLimit,
            RequireAtLeastOneFilter = dataset.RequireAtLeastOneFilter,
            RequireDateFilter = dataset.RequireDateFilter,
            LargeDatasetThreshold = dataset.LargeDatasetThreshold,
            TimeoutSeconds = dataset.TimeoutSeconds
        });

        return Ok(response);
    }

    [HttpGet("{id:guid}/fields")]
    public async Task<ActionResult<IEnumerable<DatasetFieldMetadataDto>>> GetDatasetFields(Guid id, CancellationToken cancellationToken)
    {
        var datasetExists = await _datasetRepository.DatasetExistsAsync(id, cancellationToken);
        if (!datasetExists)
        {
            return NotFound(new { message = $"Dataset with id '{id}' was not found." });
        }

        var fields = await _datasetRepository.GetDatasetFieldsAsync(id, cancellationToken);
        var response = fields.Select(field => new DatasetFieldMetadataDto
        {
            Id = field.Id,
            FieldName = field.FieldName,
            DisplayName = field.DisplayName,
            DataType = field.DataType,
            IsFilterable = field.IsFilterable,
            IsGroupable = field.IsGroupable,
            IsSummarizable = field.IsSummarizable
        });

        return Ok(response);
    }
}
