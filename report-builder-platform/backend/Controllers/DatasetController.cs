using backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/datasets")]
public class DatasetController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Dataset>> GetDatasets()
    {
        var datasets = new List<Dataset>
        {
            new()
            {
                Id = 1,
                Name = "Sales Dataset",
                Description = "Transactions and revenue metrics",
                ViewName = "vw_sales_summary"
            },
            new()
            {
                Id = 2,
                Name = "Customers Dataset",
                Description = "Customer profile and lifecycle data",
                ViewName = "vw_customers"
            }
        };

        return Ok(datasets);
    }

    [HttpGet("{id:int}/fields")]
    public ActionResult<IEnumerable<DatasetField>> GetDatasetFields(int id)
    {
        var fields = new List<DatasetField>
        {
            new()
            {
                Id = 1,
                DatasetId = id,
                FieldName = "transaction_date",
                DisplayName = "Transaction Date",
                DataType = "date",
                IsFilterable = true,
                IsGroupable = true,
                IsSummarizable = false
            },
            new()
            {
                Id = 2,
                DatasetId = id,
                FieldName = "net_revenue",
                DisplayName = "Net Revenue",
                DataType = "decimal",
                IsFilterable = true,
                IsGroupable = false,
                IsSummarizable = true
            }
        };

        return Ok(fields);
    }
}
