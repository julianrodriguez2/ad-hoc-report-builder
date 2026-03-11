using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/saved-reports")]
public class SavedReportsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<SavedReport>> GetSavedReports()
    {
        var reports = new List<SavedReport>
        {
            new()
            {
                Id = 1,
                Name = "Monthly Revenue by Region",
                DefinitionJson = "{ \"datasetId\": 1 }",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "system@demo.local"
            },
            new()
            {
                Id = 2,
                Name = "Customer Churn Overview",
                DefinitionJson = "{ \"datasetId\": 2 }",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                CreatedBy = "analyst@demo.local"
            }
        };

        return Ok(reports);
    }

    [HttpPost]
    public ActionResult<SavedReport> CreateSavedReport([FromBody] CreateSavedReportDto request)
    {
        var savedReport = new SavedReport
        {
            Id = Random.Shared.Next(1000, 9999),
            Name = request.Name,
            DefinitionJson = request.DefinitionJson,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        return CreatedAtAction(nameof(GetSavedReportById), new { id = savedReport.Id }, savedReport);
    }

    [HttpGet("{id:int}")]
    public ActionResult<SavedReport> GetSavedReportById(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var report = new SavedReport
        {
            Id = id,
            Name = "Stub Saved Report",
            DefinitionJson = "{ \"datasetId\": 1, \"fields\": [\"region\", \"net_revenue\"] }",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedBy = "system@demo.local"
        };

        return Ok(report);
    }
}
