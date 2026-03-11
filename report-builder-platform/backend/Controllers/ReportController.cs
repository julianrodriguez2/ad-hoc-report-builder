using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportController(IReportPreviewService reportPreviewService) : ControllerBase
{
    private readonly IReportPreviewService _reportPreviewService = reportPreviewService;

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
                message = "Report definition validation failed.",
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
    public ActionResult<object> ExportPdf()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "PDF export is not implemented yet."
        });
    }

    [HttpPost("export/excel")]
    public ActionResult<object> ExportExcel()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Excel export is not implemented yet."
        });
    }
}
