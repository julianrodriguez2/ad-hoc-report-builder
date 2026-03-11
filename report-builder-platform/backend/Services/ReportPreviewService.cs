using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using backend.Data;
using backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class ReportPreviewService(AppDbContext dbContext, IReportQueryBuilderService reportQueryBuilderService) : IReportPreviewService
{
    private const int PreviewRowLimit = 100;
    private const int PreviewTimeoutSeconds = 10;
    private static readonly Regex SelectClausePattern = new(@"^\s*SELECT\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IReportQueryBuilderService _reportQueryBuilderService = reportQueryBuilderService;

    public async Task<PreviewResultDto> ExecutePreviewAsync(ReportDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        var queryBuildResult = await _reportQueryBuilderService.BuildPreviewQueryAsync(definition, cancellationToken);
        var previewSql = ApplyPreviewGuardrails(queryBuildResult.Sql);

        var datasetFieldMap = await _reportQueryBuilderService.GetDatasetFieldMapAsync(definition.DatasetId, cancellationToken);
        var columns = BuildColumnMetadata(definition, datasetFieldMap);

        var rows = await ExecuteQueryAsync(previewSql, queryBuildResult.Parameters, cancellationToken);
        var rowCount = rows.Count;

        return new PreviewResultDto
        {
            Columns = columns,
            Rows = rows,
            RowCount = rowCount,
            IsTruncated = rowCount >= PreviewRowLimit,
            DebugSql = previewSql
        };
    }

    private static string ApplyPreviewGuardrails(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ReportValidationException("Generated SQL is empty.");
        }

        if (!sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReportValidationException("Preview query must include deterministic ordering.");
        }

        if (!SelectClausePattern.IsMatch(sql))
        {
            throw new ReportValidationException("Generated SQL is not in a valid SELECT format.");
        }

        return SelectClausePattern.Replace(sql, $"SELECT TOP ({PreviewRowLimit}) ", 1);
    }

    private static List<PreviewColumnDto> BuildColumnMetadata(
        ReportDefinitionDto definition,
        IReadOnlyDictionary<string, Models.DatasetField> datasetFieldMap)
    {
        var columns = new List<PreviewColumnDto>();
        foreach (var selectedField in definition.Fields ?? [])
        {
            var fieldName = selectedField.FieldName?.Trim();
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                continue;
            }

            if (!datasetFieldMap.TryGetValue(fieldName, out var metadataField))
            {
                continue;
            }

            columns.Add(new PreviewColumnDto
            {
                FieldName = metadataField.FieldName,
                DisplayName = metadataField.DisplayName
            });
        }

        return columns;
    }

    private async Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
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
            command.CommandTimeout = PreviewTimeoutSeconds;

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
