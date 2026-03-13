using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportController(
    IReportPreviewService reportPreviewService,
    IReportExportService reportExportService) : ControllerBase
{
    private readonly IReportPreviewService _reportPreviewService = reportPreviewService;
    private readonly IReportExportService _reportExportService = reportExportService;

    [HttpPost("preview")]
    public async Task<ActionResult<PreviewResultDto>> Preview([FromBody] ReportDefinitionDto definition, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _reportPreviewService.ExecutePreviewAsync(definition, cancellationToken);
            return Ok(result);
        }
        catch (ReportValidationException exception)
        {
            return BadRequest(new
            {
                message = "Report validation failed.",
                errors = exception.Errors
            });
        }
        catch (PreviewExecutionException exception) when (exception.IsTimeout)
        {
            return BadRequest(new
            {
                message = "Preview query exceeded the allowed execution time.",
                errors = new[] { "Preview query exceeded the allowed execution time." }
            });
        }
        catch (PreviewExecutionException exception)
        {
            return BadRequest(new
            {
                message = exception.Message,
                errors = new[] { exception.Message }
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while running report preview."
            });
        }
    }

    [HttpPost("run")]
    public ActionResult<object> Run()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Report execution is not implemented yet."
        });
    }

    [HttpPost("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromBody] ReportDefinitionDto definition, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _reportExportService.ExportPdfAsync(definition, cancellationToken);
            var fileName = $"report-{DateTime.UtcNow:yyyy-MM-dd}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (ReportValidationException exception)
        {
            return BadRequest(new
            {
                message = "Report validation failed.",
                errors = exception.Errors
            });
        }
        catch (ReportExportException exception) when (exception.IsTimeout)
        {
            return BadRequest(new
            {
                message = "Export query exceeded the allowed execution time.",
                errors = new[] { "Export query exceeded the allowed execution time." }
            });
        }
        catch (ReportExportException exception)
        {
            return BadRequest(new
            {
                message = exception.Message,
                errors = new[] { exception.Message }
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while exporting PDF."
            });
        }
    }

    [HttpPost("export/excel")]
    public async Task<IActionResult> ExportExcel([FromBody] ReportDefinitionDto definition, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _reportExportService.ExportExcelAsync(definition, cancellationToken);
            var fileName = $"report-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (ReportValidationException exception)
        {
            return BadRequest(new
            {
                message = "Report validation failed.",
                errors = exception.Errors
            });
        }
        catch (ReportExportException exception) when (exception.IsTimeout)
        {
            return BadRequest(new
            {
                message = "Export query exceeded the allowed execution time.",
                errors = new[] { "Export query exceeded the allowed execution time." }
            });
        }
        catch (ReportExportException exception)
        {
            return BadRequest(new
            {
                message = exception.Message,
                errors = new[] { exception.Message }
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while exporting Excel."
            });
        }
    }
}
