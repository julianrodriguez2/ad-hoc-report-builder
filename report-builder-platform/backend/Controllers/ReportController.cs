using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportController(IReportQueryBuilderService reportQueryBuilderService) : ControllerBase
{
    private readonly IReportQueryBuilderService _reportQueryBuilderService = reportQueryBuilderService;

    [HttpPost("preview")]
    public async Task<ActionResult<QueryBuildResult>> Preview([FromBody] ReportDefinitionDto definition, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _reportQueryBuilderService.BuildPreviewQueryAsync(definition, cancellationToken);
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
