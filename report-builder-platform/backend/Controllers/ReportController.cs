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
    public ActionResult<object> Preview([FromBody] ReportDefinitionDto definition)
    {
        var query = _reportQueryBuilderService.BuildPreviewQuery(definition);

        return Ok(new
        {
            sql = query,
            rows = new[]
            {
                new { transaction_date = "2026-01-01", region = "North America", net_revenue = 12000 },
                new { transaction_date = "2026-01-02", region = "Europe", net_revenue = 9800 }
            }
        });
    }

    [HttpPost("run")]
    public ActionResult<object> Run([FromBody] ReportDefinitionDto definition)
    {
        var query = _reportQueryBuilderService.BuildFullQuery(definition);

        return Ok(new
        {
            sql = query,
            totalRows = 200,
            status = "Stub execution completed"
        });
    }

    [HttpPost("export/pdf")]
    public ActionResult<object> ExportPdf([FromBody] ReportDefinitionDto definition)
    {
        _ = _reportQueryBuilderService.BuildFullQuery(definition);

        return Ok(new
        {
            message = "PDF export queued (stub)",
            fileToken = Guid.NewGuid().ToString("N")
        });
    }

    [HttpPost("export/excel")]
    public ActionResult<object> ExportExcel([FromBody] ReportDefinitionDto definition)
    {
        _ = _reportQueryBuilderService.BuildFullQuery(definition);

        return Ok(new
        {
            message = "Excel export queued (stub)",
            fileToken = Guid.NewGuid().ToString("N")
        });
    }
}
