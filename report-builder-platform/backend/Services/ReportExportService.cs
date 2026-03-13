using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using backend.Data;
using backend.DTOs;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace backend.Services;

public class ReportExportService(
    AppDbContext dbContext,
    IReportQueryBuilderService reportQueryBuilderService,
    IReportGuardrailService reportGuardrailService,
    IHttpClientFactory httpClientFactory) : IReportExportService
{
    private static readonly Regex SelectClausePattern = new(@"^\s*SELECT\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex InvalidWorksheetNameCharacters = new(@"[\[\]\*\/\\\?:]", RegexOptions.Compiled);

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IReportQueryBuilderService _reportQueryBuilderService = reportQueryBuilderService;
    private readonly IReportGuardrailService _reportGuardrailService = reportGuardrailService;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<byte[]> ExportPdfAsync(ReportDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        var exportData = await LoadExportDataAsync(definition, cancellationToken);
        return BuildPdfDocument(exportData);
    }

    public async Task<byte[]> ExportExcelAsync(ReportDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        var exportData = await LoadExportDataAsync(definition, cancellationToken);
        return BuildExcelDocument(exportData);
    }

    private async Task<ExportData> LoadExportDataAsync(ReportDefinitionDto definition, CancellationToken cancellationToken)
    {
        var generatedAtUtc = DateTime.UtcNow;
        var dataset = await _reportQueryBuilderService.GetValidatedDatasetAsync(definition.DatasetId, cancellationToken);
        var datasetFieldMap = await _reportQueryBuilderService.GetDatasetFieldMapAsync(definition.DatasetId, cancellationToken);
        var guardrailSettings = _reportGuardrailService.ValidateExecutionRequest(definition, dataset, datasetFieldMap);
        var queryBuildResult = await _reportQueryBuilderService.BuildPreviewQueryAsync(definition, cancellationToken);

        var rowLimit = guardrailSettings.MaxExecutionRowLimit;
        var guardedSql = ApplyExecutionGuardrails(queryBuildResult.Sql, rowLimit + 1);

        var rows = await ExecuteQueryAsync(
            guardedSql,
            queryBuildResult.Parameters,
            guardrailSettings.TimeoutSeconds,
            cancellationToken);

        var isTruncated = rows.Count > rowLimit;
        if (isTruncated)
        {
            rows = rows.Take(rowLimit).ToList();
        }

        var columns = ReportColumnMetadataBuilder.BuildColumns(definition, datasetFieldMap);
        var layoutSettings = NormalizeLayoutSettings(definition.LayoutSettings);
        var logoImageBytes = await TryLoadLogoImageBytesAsync(layoutSettings.LogoUrl, cancellationToken);

        return new ExportData(
            columns,
            rows,
            layoutSettings,
            generatedAtUtc,
            isTruncated,
            rowLimit,
            logoImageBytes);
    }

    private static string ApplyExecutionGuardrails(string sql, int maxRowsWithMarker)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ReportValidationException("Generated SQL is empty.");
        }

        if (maxRowsWithMarker <= 1)
        {
            throw new ReportValidationException("Execution row limit must be greater than zero.");
        }

        if (!sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReportValidationException("Export query must include deterministic ordering.");
        }

        if (!SelectClausePattern.IsMatch(sql))
        {
            throw new ReportValidationException("Generated SQL is not in a valid SELECT format.");
        }

        return SelectClausePattern.Replace(sql, $"SELECT TOP ({maxRowsWithMarker}) ", 1);
    }

    private async Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = false;

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
            shouldCloseConnection = true;
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = timeoutSeconds;

            foreach (var parameter in parameters)
            {
                var dbParameter = command.CreateParameter();
                dbParameter.ParameterName = parameter.Key;
                dbParameter.Value = NormalizeParameterValue(parameter.Value) ?? DBNull.Value;
                command.Parameters.Add(dbParameter);
            }

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await ReadRowsAsync(reader, cancellationToken);
        }
        catch (Exception exception) when (IsTimeoutException(exception))
        {
            throw new ReportExportException("Export query exceeded the allowed execution time.", isTimeout: true, exception);
        }
        catch (DbException exception)
        {
            throw new ReportExportException(
                "Export query execution failed. Ensure the dataset view exists and is accessible.",
                isTimeout: false,
                exception);
        }
        finally
        {
            if (shouldCloseConnection && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < reader.FieldCount; index++)
            {
                row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
            }

            rows.Add(row);
        }

        return rows;
    }

    private byte[] BuildPdfDocument(ExportData exportData)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pageSize = ResolvePageSize(exportData.LayoutSettings.PageSize, exportData.LayoutSettings.PageOrientation);

        return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(pageSize);
                    page.Margin(24);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header().Element(header => ComposePdfHeader(header, exportData));
                    page.Content().Element(content => ComposePdfTable(content, exportData));
                    page.Footer().Element(footer => ComposePdfFooter(footer, exportData.LayoutSettings));
                });
            })
            .GeneratePdf();
    }

    private static void ComposePdfHeader(IContainer container, ExportData exportData)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Row(row =>
            {
                if (exportData.LogoImageBytes is { Length: > 0 })
                {
                    row.ConstantItem(90).Height(42).Image(exportData.LogoImageBytes).FitArea();
                }

                row.RelativeItem().Column(textColumn =>
                {
                    textColumn.Spacing(2);
                    textColumn.Item().Text(exportData.LayoutSettings.ReportTitle).SemiBold().FontSize(16);

                    if (!string.IsNullOrWhiteSpace(exportData.LayoutSettings.Subtitle))
                    {
                        textColumn.Item().Text(exportData.LayoutSettings.Subtitle!).FontSize(11).FontColor(Colors.Grey.Darken1);
                    }

                    if (!string.IsNullOrWhiteSpace(exportData.LayoutSettings.HeaderText))
                    {
                        textColumn.Item().Text(exportData.LayoutSettings.HeaderText!).FontSize(9).FontColor(Colors.Grey.Darken2);
                    }

                    if (exportData.LayoutSettings.ShowGeneratedDate)
                    {
                        textColumn.Item()
                            .Text($"Generated: {exportData.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);
                    }
                });
            });
        });
    }

    private static void ComposePdfTable(IContainer container, ExportData exportData)
    {
        if (exportData.Columns.Count == 0)
        {
            container.Text("No columns available to export.");
            return;
        }

        container.Column(column =>
        {
            column.Spacing(8);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in exportData.Columns)
                    {
                        columns.RelativeColumn();
                    }
                });

                table.Header(header =>
                {
                    foreach (var previewColumn in exportData.Columns)
                    {
                        header.Cell().Element(ComposePdfHeaderCell).Text(previewColumn.DisplayName).SemiBold();
                    }
                });

                foreach (var row in exportData.Rows)
                {
                    foreach (var previewColumn in exportData.Columns)
                    {
                        row.TryGetValue(previewColumn.FieldName, out var value);
                        table.Cell().Element(ComposePdfBodyCell).Text(FormatValueForText(value));
                    }
                }
            });

            if (exportData.Rows.Count == 0)
            {
                column.Item().Text("No records matched the selected report definition.").FontColor(Colors.Grey.Darken2);
            }

            if (exportData.IsTruncated)
            {
                column.Item()
                    .Text($"Results truncated to the first {exportData.AppliedRowLimit} rows due to dataset execution limits.")
                    .FontSize(9)
                    .Italic()
                    .FontColor(Colors.Orange.Darken2);
            }
        });
    }

    private static void ComposePdfFooter(IContainer container, ResolvedLayoutSettings layoutSettings)
    {
        container.Row(row =>
        {
            row.RelativeItem().AlignLeft().Text(text =>
            {
                if (!string.IsNullOrWhiteSpace(layoutSettings.FooterText))
                {
                    text.Span(layoutSettings.FooterText).FontSize(9).FontColor(Colors.Grey.Darken2);
                }
            });

            row.RelativeItem().AlignRight().Text(text =>
            {
                if (!layoutSettings.ShowPageNumbers)
                {
                    return;
                }

                text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Darken2);
                text.Span(" / ").FontSize(9).FontColor(Colors.Grey.Darken2);
                text.TotalPages().FontSize(9).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private byte[] BuildExcelDocument(ExportData exportData)
    {
        using var workbook = new XLWorkbook();
        var columnCount = Math.Max(1, exportData.Columns.Count);
        var worksheetName = BuildWorksheetName(exportData.LayoutSettings.ReportTitle);
        var worksheet = workbook.Worksheets.Add(worksheetName);

        worksheet.Cell(1, 1).Value = exportData.LayoutSettings.ReportTitle;
        worksheet.Range(1, 1, 1, columnCount).Merge();
        worksheet.Range(1, 1, 1, columnCount).Style.Font.SetBold();
        worksheet.Range(1, 1, 1, columnCount).Style.Font.FontSize = 14;

        if (exportData.LayoutSettings.ShowGeneratedDate)
        {
            worksheet.Cell(2, 1).Value = $"Generated: {exportData.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC";
            worksheet.Range(2, 1, 2, columnCount).Merge();
        }

        var thirdRowText = !string.IsNullOrWhiteSpace(exportData.LayoutSettings.Subtitle)
            ? exportData.LayoutSettings.Subtitle!
            : exportData.LayoutSettings.HeaderText;
        if (!string.IsNullOrWhiteSpace(thirdRowText))
        {
            worksheet.Cell(3, 1).Value = thirdRowText;
            worksheet.Range(3, 1, 3, columnCount).Merge();
        }

        const int headerRow = 4;
        for (var columnIndex = 0; columnIndex < exportData.Columns.Count; columnIndex++)
        {
            worksheet.Cell(headerRow, columnIndex + 1).Value = exportData.Columns[columnIndex].DisplayName;
        }

        var headerRange = worksheet.Range(headerRow, 1, headerRow, columnCount);
        headerRange.Style.Font.SetBold();
        headerRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E7EDF5"));
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var dataStartRow = headerRow + 1;
        for (var rowIndex = 0; rowIndex < exportData.Rows.Count; rowIndex++)
        {
            var currentRowNumber = dataStartRow + rowIndex;
            var row = exportData.Rows[rowIndex];

            for (var columnIndex = 0; columnIndex < exportData.Columns.Count; columnIndex++)
            {
                var column = exportData.Columns[columnIndex];
                row.TryGetValue(column.FieldName, out var value);
                worksheet.Cell(currentRowNumber, columnIndex + 1).Value = FormatValueForCell(value);
            }
        }

        var dataEndRow = dataStartRow + Math.Max(0, exportData.Rows.Count - 1);
        if (exportData.Rows.Count > 0)
        {
            var tableRange = worksheet.Range(headerRow, 1, dataEndRow, columnCount);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        var noteRow = Math.Max(dataEndRow + 2, dataStartRow + 1);
        if (exportData.IsTruncated)
        {
            worksheet.Cell(noteRow, 1).Value =
                $"Results truncated to the first {exportData.AppliedRowLimit} rows due to dataset execution limits.";
            worksheet.Range(noteRow, 1, noteRow, columnCount).Merge();
            noteRow++;
        }

        if (!string.IsNullOrWhiteSpace(exportData.LayoutSettings.FooterText))
        {
            worksheet.Cell(noteRow, 1).Value = exportData.LayoutSettings.FooterText;
            worksheet.Range(noteRow, 1, noteRow, columnCount).Merge();
        }

        worksheet.Columns(1, columnCount).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<byte[]?> TryLoadLogoImageBytesAsync(string? logoUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var response = await client.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return imageBytes.Length == 0 ? null : imageBytes;
        }
        catch
        {
            return null;
        }
    }

    private static PageSize ResolvePageSize(string pageSize, string pageOrientation)
    {
        var normalizedPageSize = pageSize.Trim().ToLowerInvariant();
        var normalizedOrientation = pageOrientation.Trim().ToLowerInvariant();
        var baseSize = normalizedPageSize == "letter" ? PageSizes.Letter : PageSizes.A4;

        return normalizedOrientation == "landscape" ? baseSize.Landscape() : baseSize;
    }

    private static ResolvedLayoutSettings NormalizeLayoutSettings(LayoutSettingsDto? layoutSettings)
    {
        var normalized = layoutSettings ?? new LayoutSettingsDto();
        var reportTitle = string.IsNullOrWhiteSpace(normalized.ReportTitle)
            ? "Report Export"
            : normalized.ReportTitle.Trim();
        var templateId = string.IsNullOrWhiteSpace(normalized.TemplateId)
            ? "simple-table"
            : normalized.TemplateId.Trim();
        var pageOrientation = normalized.PageOrientation?.Trim().ToLowerInvariant() == "landscape"
            ? "landscape"
            : "portrait";
        var pageSize = normalized.PageSize?.Trim().Equals("Letter", StringComparison.OrdinalIgnoreCase) == true
            ? "Letter"
            : "A4";

        return new ResolvedLayoutSettings(
            templateId,
            reportTitle,
            NormalizeOptionalText(normalized.Subtitle),
            NormalizeOptionalText(normalized.LogoUrl),
            NormalizeOptionalText(normalized.HeaderText),
            NormalizeOptionalText(normalized.FooterText),
            pageOrientation,
            pageSize,
            normalized.ShowGeneratedDate,
            normalized.ShowPageNumbers);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static IContainer ComposePdfHeaderCell(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(5)
            .PaddingHorizontal(6);
    }

    private static IContainer ComposePdfBodyCell(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(6);
    }

    private static string FormatValueForText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            bool boolValue => boolValue ? "True" : "False",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static XLCellValue FormatValueForCell(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime,
            bool boolValue => boolValue,
            byte byteValue => byteValue,
            short shortValue => shortValue,
            int intValue => intValue,
            long longValue => longValue,
            decimal decimalValue => decimalValue,
            float floatValue => floatValue,
            double doubleValue => doubleValue,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string BuildWorksheetName(string reportTitle)
    {
        var candidate = string.IsNullOrWhiteSpace(reportTitle) ? "Report" : reportTitle.Trim();
        candidate = InvalidWorksheetNameCharacters.Replace(candidate, " ");
        candidate = candidate.Length > 31 ? candidate[..31] : candidate;
        return string.IsNullOrWhiteSpace(candidate) ? "Report" : candidate;
    }

    private static bool IsTimeoutException(Exception exception)
    {
        var current = exception;
        while (current is not null)
        {
            if (current is TimeoutException)
            {
                return true;
            }

            if (current is DbException dbException &&
                dbException.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.InnerException!;
        }

        return false;
    }

    private static object? NormalizeParameterValue(object? value)
    {
        if (value is DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }

        return value;
    }

    private sealed record ExportData(
        IReadOnlyList<PreviewColumnDto> Columns,
        List<Dictionary<string, object?>> Rows,
        ResolvedLayoutSettings LayoutSettings,
        DateTime GeneratedAtUtc,
        bool IsTruncated,
        int AppliedRowLimit,
        byte[]? LogoImageBytes);

    private sealed record ResolvedLayoutSettings(
        string TemplateId,
        string ReportTitle,
        string? Subtitle,
        string? LogoUrl,
        string? HeaderText,
        string? FooterText,
        string PageOrientation,
        string PageSize,
        bool ShowGeneratedDate,
        bool ShowPageNumbers);
}
