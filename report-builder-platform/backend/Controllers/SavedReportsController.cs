using System.Text.Json;
using backend.DTOs;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/saved-reports")]
public class SavedReportsController(
    ISavedReportRepository savedReportRepository,
    IDatasetRepository datasetRepository) : ControllerBase
{
    private readonly ISavedReportRepository _savedReportRepository = savedReportRepository;
    private readonly IDatasetRepository _datasetRepository = datasetRepository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SavedReportSummaryDto>>> GetSavedReports(CancellationToken cancellationToken)
    {
        var reports = await _savedReportRepository.GetSavedReportsAsync(cancellationToken);
        var response = reports.Select(report => new SavedReportSummaryDto
        {
            Id = report.Id,
            Name = report.Name,
            Description = report.Description,
            DatasetId = report.DatasetId,
            CreatedAt = report.CreatedAt
        });

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SavedReportDto>> GetSavedReportById(Guid id, CancellationToken cancellationToken)
    {
        var report = await _savedReportRepository.GetSavedReportByIdAsync(id, cancellationToken);
        if (report is null)
        {
            return NotFound(new { message = $"Saved report '{id}' was not found." });
        }

        return Ok(MapToDto(report));
    }

    [HttpPost]
    public async Task<ActionResult<SavedReportDto>> CreateSavedReport(
        [FromBody] CreateSavedReportRequest request,
        CancellationToken cancellationToken)
    {
        var errors = await ValidateCreateRequestAsync(request, cancellationToken);
        if (errors.Count > 0)
        {
            return BadRequest(new
            {
                message = "Saved report validation failed.",
                errors
            });
        }

        var report = new SavedReport
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            DatasetId = request.DatasetId,
            DefinitionJson = NormalizeDefinitionJson(request.Definition),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            CreatedBy = "system"
        };

        var createdReport = await _savedReportRepository.AddSavedReportAsync(report, cancellationToken);
        return CreatedAtAction(nameof(GetSavedReportById), new { id = createdReport.Id }, MapToDto(createdReport));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SavedReportDto>> UpdateSavedReport(
        Guid id,
        [FromBody] UpdateSavedReportRequest request,
        CancellationToken cancellationToken)
    {
        var existingReport = await _savedReportRepository.GetSavedReportByIdForUpdateAsync(id, cancellationToken);
        if (existingReport is null)
        {
            return NotFound(new { message = $"Saved report '{id}' was not found." });
        }

        var errors = ValidateUpdateRequest(request, existingReport.DatasetId);
        if (errors.Count > 0)
        {
            return BadRequest(new
            {
                message = "Saved report validation failed.",
                errors
            });
        }

        existingReport.Name = request.Name.Trim();
        existingReport.Description = NormalizeDescription(request.Description);
        existingReport.DefinitionJson = NormalizeDefinitionJson(request.Definition);
        existingReport.UpdatedAt = DateTime.UtcNow;

        await _savedReportRepository.SaveChangesAsync(cancellationToken);
        return Ok(MapToDto(existingReport));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSavedReport(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _savedReportRepository.DeleteSavedReportAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Saved report '{id}' was not found." });
        }

        return NoContent();
    }

    private async Task<List<string>> ValidateCreateRequestAsync(CreateSavedReportRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Report name is required.");
        }

        if (request.DatasetId == Guid.Empty)
        {
            errors.Add("DatasetId is required.");
            return errors;
        }

        if (!HasDefinitionObject(request.Definition))
        {
            errors.Add("Definition must be a valid JSON object.");
            return errors;
        }

        var datasetExists = await _datasetRepository.DatasetExistsAsync(request.DatasetId, cancellationToken);
        if (!datasetExists)
        {
            errors.Add($"Dataset '{request.DatasetId}' does not exist.");
        }

        if (!TryGetDefinitionDatasetId(request.Definition, out var definitionDatasetId))
        {
            errors.Add("Definition.datasetId is required and must be a valid GUID.");
        }
        else if (definitionDatasetId != request.DatasetId)
        {
            errors.Add("DatasetId in request must match definition.datasetId.");
        }

        return errors;
    }

    private List<string> ValidateUpdateRequest(UpdateSavedReportRequest request, Guid existingDatasetId)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Report name is required.");
        }

        if (!HasDefinitionObject(request.Definition))
        {
            errors.Add("Definition must be a valid JSON object.");
            return errors;
        }

        if (!TryGetDefinitionDatasetId(request.Definition, out var definitionDatasetId))
        {
            errors.Add("Definition.datasetId is required and must be a valid GUID.");
        }
        else if (definitionDatasetId != existingDatasetId)
        {
            errors.Add("Definition.datasetId cannot be changed for an existing saved report.");
        }

        return errors;
    }

    private static SavedReportDto MapToDto(SavedReport report)
    {
        return new SavedReportDto
        {
            Id = report.Id,
            Name = report.Name,
            Description = report.Description,
            DatasetId = report.DatasetId,
            DefinitionJson = report.DefinitionJson,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    private static bool HasDefinitionObject(JsonElement definition)
    {
        return definition.ValueKind == JsonValueKind.Object && definition.EnumerateObject().Any();
    }

    private static string NormalizeDefinitionJson(JsonElement definition)
    {
        return definition.GetRawText();
    }

    private static bool TryGetDefinitionDatasetId(JsonElement definition, out Guid datasetId)
    {
        datasetId = Guid.Empty;
        if (definition.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in definition.EnumerateObject())
        {
            if (!property.Name.Equals("datasetId", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String &&
                Guid.TryParse(property.Value.GetString(), out datasetId))
            {
                return true;
            }

            return false;
        }

        return false;
    }

    private static string? NormalizeDescription(string? description)
    {
        var trimmed = description?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
