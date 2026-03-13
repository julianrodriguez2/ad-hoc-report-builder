using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using backend.Data;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class ReportPreviewService(
    AppDbContext dbContext,
    IReportQueryBuilderService reportQueryBuilderService,
    IReportGuardrailService reportGuardrailService) : IReportPreviewService
{
    private static readonly Regex SelectClausePattern = new(@"^\s*SELECT\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IReportQueryBuilderService _reportQueryBuilderService = reportQueryBuilderService;
    private readonly IReportGuardrailService _reportGuardrailService = reportGuardrailService;

    public async Task<PreviewResultDto> ExecutePreviewAsync(ReportDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataset = await _reportQueryBuilderService.GetValidatedDatasetAsync(definition.DatasetId, cancellationToken);
        var datasetFieldMap = await _reportQueryBuilderService.GetDatasetFieldMapAsync(definition.DatasetId, cancellationToken);
        var guardrailSettings = _reportGuardrailService.ValidatePreviewRequest(definition, dataset, datasetFieldMap);

        var queryBuildResult = await _reportQueryBuilderService.BuildPreviewQueryAsync(definition, cancellationToken);
        var previewSql = ApplyPreviewGuardrails(queryBuildResult.Sql, guardrailSettings.PreviewRowLimit);

        var columns = ReportColumnMetadataBuilder.BuildColumns(definition, datasetFieldMap);

        var rows = await ExecuteQueryAsync(
            previewSql,
            queryBuildResult.Parameters,
            guardrailSettings.TimeoutSeconds,
            cancellationToken);
        var rowCount = rows.Count;
        stopwatch.Stop();

        return new PreviewResultDto
        {
            Columns = columns,
            Rows = rows,
            RowCount = rowCount,
            IsTruncated = rowCount >= guardrailSettings.PreviewRowLimit,
            AppliedRowLimit = guardrailSettings.PreviewRowLimit,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            DebugSql = previewSql
        };
    }

    private static string ApplyPreviewGuardrails(string sql, int previewRowLimit)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ReportValidationException("Generated SQL is empty.");
        }

        if (previewRowLimit <= 0)
        {
            throw new ReportValidationException("Preview row limit must be greater than zero.");
        }

        if (!sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReportValidationException("Preview query must include deterministic ordering.");
        }

        if (!SelectClausePattern.IsMatch(sql))
        {
            throw new ReportValidationException("Generated SQL is not in a valid SELECT format.");
        }

        return SelectClausePattern.Replace(sql, $"SELECT TOP ({previewRowLimit}) ", 1);
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
            throw new PreviewExecutionException("Preview query exceeded the allowed execution time.", isTimeout: true, exception);
        }
        catch (DbException exception)
        {
            throw new PreviewExecutionException(
                "Preview query execution failed. Ensure the dataset view exists and is accessible.",
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
            for (var columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
            {
                var value = reader.IsDBNull(columnIndex) ? null : reader.GetValue(columnIndex);
                row[reader.GetName(columnIndex)] = value;
            }

            rows.Add(row);
        }

        return rows;
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
}
