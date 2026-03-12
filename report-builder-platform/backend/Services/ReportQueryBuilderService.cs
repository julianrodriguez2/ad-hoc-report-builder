using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class ReportQueryBuilderService(AppDbContext dbContext) : IReportQueryBuilderService
{
    private readonly AppDbContext _dbContext = dbContext;

    private static readonly Regex SqlIdentifierPattern = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private static readonly Regex SqlIdentifierPathPattern = new("^[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.Compiled);

    private static readonly HashSet<string> OperatorsWithoutValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "isBlank",
        "isNotBlank",
        "isNull",
        "isNotNull"
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedOperatorsByDataType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "equals", "notEquals", "contains", "startsWith", "endsWith", "isBlank", "isNotBlank"
        },
        ["number"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "equals", "notEquals", "greaterThan", "greaterThanOrEqual", "lessThan", "lessThanOrEqual", "between", "isNull", "isNotNull"
        },
        ["date"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "equals", "before", "after", "between", "isNull", "isNotNull"
        },
        ["boolean"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "equals"
        }
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedAggregationsByDataType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["string"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "count", "min", "max"
        },
        ["number"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "count", "sum", "avg", "min", "max"
        },
        ["date"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "count", "min", "max"
        },
        ["boolean"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "count"
        }
    };

    // Example definitions for local testing:
    // 1) Dataset Permits -> Fields PermitNumber, ApplicantName -> Filter Status equals Active
    // 2) Dataset Permits -> Fields PermitNumber, IssueDate -> Filter IssueDate after 2024-01-01
    // 3) Dataset Inspections -> Fields InspectionId, InspectorName -> Filter InspectionStatus contains Passed
    public async Task<QueryBuildResult> BuildPreviewQueryAsync(ReportDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        var definitionErrors = ValidateDefinition(definition);
        if (definitionErrors.Count > 0)
        {
            throw new ReportValidationException(definitionErrors);
        }

        var dataset = await GetValidatedDatasetAsync(definition.DatasetId, cancellationToken);
        var datasetFieldMap = await GetDatasetFieldMapAsync(definition.DatasetId, cancellationToken);

        var requestedFields = definition.Fields ?? [];
        var requestedFilters = definition.Filters ?? [];
        var requestedGrouping = definition.Grouping ?? [];
        var requestedSummaries = definition.Summaries ?? [];

        var selectedFields = ValidateFieldSelection(requestedFields, datasetFieldMap);
        var validatedFilters = ValidateFilters(requestedFilters, datasetFieldMap);
        var validatedGrouping = ValidateGrouping(requestedGrouping, datasetFieldMap);
        var validatedSummaries = ValidateSummaries(requestedSummaries, datasetFieldMap, validatedGrouping);
        ValidateGroupingSelectionCompatibility(selectedFields, validatedGrouping, validatedSummaries);

        var parameters = new Dictionary<string, object?>();
        var selectClause = BuildSelectClause(selectedFields, validatedGrouping, validatedSummaries);
        var fromClause = $"FROM {QuoteIdentifierIfNeeded(dataset.ViewName, allowPath: true)}";
        var whereClause = BuildWhereClause(validatedFilters, parameters);
        var groupByClause = BuildGroupByClause(validatedGrouping);
        var orderByClause = BuildOrderByClause(selectedFields, validatedGrouping, validatedSummaries);

        var sqlParts = new List<string> { selectClause, fromClause };
        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sqlParts.Add(whereClause);
        }

        if (!string.IsNullOrWhiteSpace(groupByClause))
        {
            sqlParts.Add(groupByClause);
        }

        sqlParts.Add(orderByClause);

        return new QueryBuildResult
        {
            Sql = string.Join(Environment.NewLine, sqlParts),
            Parameters = parameters
        };
    }

    public async Task<Dataset> GetValidatedDatasetAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        var dataset = await _dbContext.Datasets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == datasetId, cancellationToken);

        if (dataset is null)
        {
            throw new ReportValidationException($"Dataset '{datasetId}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(dataset.ViewName))
        {
            throw new ReportValidationException($"Dataset '{dataset.Name}' has no view name configured.");
        }

        if (!SqlIdentifierPathPattern.IsMatch(dataset.ViewName))
        {
            throw new ReportValidationException($"Dataset '{dataset.Name}' has an invalid view name '{dataset.ViewName}'.");
        }

        return dataset;
    }

    public async Task<Dictionary<string, DatasetField>> GetDatasetFieldMapAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        var fields = await _dbContext.DatasetFields
            .AsNoTracking()
            .Where(field => field.DatasetId == datasetId)
            .ToListAsync(cancellationToken);

        if (fields.Count == 0)
        {
            throw new ReportValidationException("The selected dataset has no metadata fields configured.");
        }

        var errors = new List<string>();
        var fieldMap = new Dictionary<string, DatasetField>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.FieldName))
            {
                errors.Add($"Dataset metadata contains a field with an empty field name (Field Id: {field.Id}).");
                continue;
            }

            if (!SqlIdentifierPattern.IsMatch(field.FieldName))
            {
                errors.Add($"Dataset metadata field '{field.FieldName}' is not a valid SQL identifier.");
                continue;
            }

            if (!fieldMap.TryAdd(field.FieldName, field))
            {
                errors.Add($"Dataset metadata contains duplicate field name '{field.FieldName}'.");
            }
        }

        if (errors.Count > 0)
        {
            throw new ReportValidationException(errors);
        }

        return fieldMap;
    }

    private static List<string> ValidateDefinition(ReportDefinitionDto definition)
    {
        var errors = new List<string>();

        if (definition.DatasetId == Guid.Empty)
        {
            errors.Add("DatasetId is required.");
        }

        return errors;
    }

    private List<DatasetField> ValidateFieldSelection(IReadOnlyList<SelectedFieldDto> selectedFields, IReadOnlyDictionary<string, DatasetField> datasetFieldMap)
    {
        var errors = new List<string>();
        var resolvedFields = new List<DatasetField>();

        if (selectedFields.Count == 0)
        {
            errors.Add("At least one selected field is required to build a preview query.");
            throw new ReportValidationException(errors);
        }

        var seenFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < selectedFields.Count; index++)
        {
            var selectedField = selectedFields[index];
            var fieldName = selectedField.FieldName?.Trim();

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                errors.Add($"Selected field at index {index} is missing FieldName.");
                continue;
            }

            if (!seenFieldNames.Add(fieldName))
            {
                errors.Add($"Selected field '{fieldName}' is duplicated.");
                continue;
            }

            if (!datasetFieldMap.TryGetValue(fieldName, out var metadataField))
            {
                errors.Add($"Selected field '{fieldName}' does not belong to the selected dataset.");
                continue;
            }

            resolvedFields.Add(metadataField);
        }

        if (errors.Count > 0)
        {
            throw new ReportValidationException(errors);
        }

        return resolvedFields;
    }

    private List<ValidatedGrouping> ValidateGrouping(
        IReadOnlyList<GroupDefinitionDto> grouping,
        IReadOnlyDictionary<string, DatasetField> datasetFieldMap)
    {
        if (grouping.Count == 0)
        {
            return [];
        }

        var normalizedGrouping = NormalizeGrouping(grouping);
        var errors = new List<string>();
        var validatedGrouping = new List<ValidatedGrouping>();
        var seenFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < normalizedGrouping.Count; index++)
        {
            var group = normalizedGrouping[index];
            var fieldName = group.FieldName?.Trim();
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                errors.Add($"Grouping item at index {index} is missing FieldName.");
                continue;
            }

            if (!seenFieldNames.Add(fieldName))
            {
                errors.Add($"Grouping field '{fieldName}' is duplicated.");
                continue;
            }

            if (!datasetFieldMap.TryGetValue(fieldName, out var metadataField))
            {
                errors.Add($"Grouping field '{fieldName}' does not belong to the selected dataset.");
                continue;
            }

            if (!metadataField.IsGroupable)
            {
                errors.Add($"Field '{fieldName}' is not marked as groupable in metadata.");
                continue;
            }

            var sortDirection = NormalizeSortDirection(group.SortDirection);
            if (sortDirection is null)
            {
                errors.Add($"Grouping field '{fieldName}' has invalid sort direction '{group.SortDirection}'. Allowed values are 'asc' or 'desc'.");
                continue;
            }

            validatedGrouping.Add(new ValidatedGrouping(metadataField, sortDirection, index + 1));
        }

        if (errors.Count > 0)
        {
            throw new ReportValidationException(errors);
        }

        return validatedGrouping;
    }

    private List<ValidatedSummary> ValidateSummaries(
        IReadOnlyList<SummaryDefinitionDto> summaries,
        IReadOnlyDictionary<string, DatasetField> datasetFieldMap,
        IReadOnlyList<ValidatedGrouping> grouping)
    {
        if (summaries.Count == 0)
        {
            return [];
        }

        if (grouping.Count == 0)
        {
            throw new ReportValidationException("Summaries currently require at least one grouping field.");
        }

        var normalizedSummaries = NormalizeSummaries(summaries);
        var errors = new List<string>();
        var validatedSummaries = new List<ValidatedSummary>();
        var seenAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < normalizedSummaries.Count; index++)
        {
            var summary = normalizedSummaries[index];
            var fieldName = summary.FieldName?.Trim();
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                errors.Add($"Summary at index {index} is missing FieldName.");
                continue;
            }

            if (!datasetFieldMap.TryGetValue(fieldName, out var metadataField))
            {
                errors.Add($"Summary field '{fieldName}' does not belong to the selected dataset.");
                continue;
            }

            if (!metadataField.IsSummarizable)
            {
                errors.Add($"Field '{fieldName}' is not marked as summarizable in metadata.");
                continue;
            }

            var aggregation = summary.Aggregation?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(aggregation))
            {
                errors.Add($"Summary for field '{fieldName}' is missing an aggregation.");
                continue;
            }

            var normalizedDataType = NormalizeDataType(metadataField.DataType);
            if (!ValidateAggregationForType(normalizedDataType, aggregation))
            {
                errors.Add(
                    $"Aggregation '{aggregation}' is not allowed for summary field '{fieldName}' with data type '{normalizedDataType}'.");
                continue;
            }

            var alias = summary.Alias?.Trim();
            if (string.IsNullOrWhiteSpace(alias))
            {
                errors.Add($"Summary for field '{fieldName}' is missing Alias.");
                continue;
            }

            if (!SqlIdentifierPattern.IsMatch(alias))
            {
                errors.Add($"Summary alias '{alias}' is invalid. Use letters, numbers, and underscores only.");
                continue;
            }

            if (datasetFieldMap.ContainsKey(alias))
            {
                errors.Add($"Summary alias '{alias}' cannot match an existing dataset field name.");
                continue;
            }

            if (!seenAliases.Add(alias))
            {
                errors.Add($"Summary alias '{alias}' is duplicated.");
                continue;
            }

            validatedSummaries.Add(new ValidatedSummary(metadataField, aggregation, alias, index + 1));
        }

        if (errors.Count > 0)
        {
            throw new ReportValidationException(errors);
        }

        return validatedSummaries;
    }

    private static void ValidateGroupingSelectionCompatibility(
        IReadOnlyList<DatasetField> selectedFields,
        IReadOnlyList<ValidatedGrouping> grouping,
        IReadOnlyList<ValidatedSummary> summaries)
    {
        if (grouping.Count == 0)
        {
            return;
        }

        if (selectedFields.Count != grouping.Count)
        {
            throw new ReportValidationException(
                summaries.Count > 0
                    ? "When summaries are used, selected fields must match grouped fields."
                    : "When grouping is used without summaries, selected fields must match grouped fields.");
        }

        var selectedFieldSet = new HashSet<string>(selectedFields.Select(field => field.FieldName), StringComparer.OrdinalIgnoreCase);
        var groupingFieldSet = new HashSet<string>(grouping.Select(group => group.Field.FieldName), StringComparer.OrdinalIgnoreCase);

        if (!selectedFieldSet.SetEquals(groupingFieldSet))
        {
            throw new ReportValidationException(
                summaries.Count > 0
                    ? "When summaries are used, selected fields must match grouped fields."
                    : "When grouping is used without summaries, selected fields must match grouped fields.");
        }
    }

    private List<ValidatedFilter> ValidateFilters(IReadOnlyList<FilterDefinitionDto> filters, IReadOnlyDictionary<string, DatasetField> datasetFieldMap)
    {
        var errors = new List<string>();
        var validatedFilters = new List<ValidatedFilter>();

        for (var index = 0; index < filters.Count; index++)
        {
            var filter = filters[index];
            var fieldName = filter.FieldName?.Trim();
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                errors.Add($"Filter at index {index} is missing FieldName.");
                continue;
            }

            if (!datasetFieldMap.TryGetValue(fieldName, out var metadataField))
            {
                errors.Add($"Filter field '{fieldName}' does not belong to the selected dataset.");
                continue;
            }

            if (!metadataField.IsFilterable)
            {
                errors.Add($"Field '{fieldName}' is not marked as filterable in metadata.");
                continue;
            }

            var @operator = filter.Operator?.Trim();
            if (string.IsNullOrWhiteSpace(@operator))
            {
                errors.Add($"Filter on field '{fieldName}' is missing an operator.");
                continue;
            }

            var normalizedDataType = NormalizeDataType(metadataField.DataType);
            if (!ValidateOperatorForDataType(normalizedDataType, @operator))
            {
                errors.Add($"Operator '{@operator}' is not allowed for field '{fieldName}' with data type '{normalizedDataType}'.");
                continue;
            }

            if (RequiresValue(@operator))
            {
                if (!TryNormalizeValue(filter.Value, normalizedDataType, @operator, out var primaryValue, out var secondaryValue, out var normalizationError))
                {
                    errors.Add($"Filter on field '{fieldName}' has invalid value: {normalizationError}");
                    continue;
                }

                validatedFilters.Add(new ValidatedFilter(metadataField, normalizedDataType, @operator, primaryValue, secondaryValue));
                continue;
            }

            validatedFilters.Add(new ValidatedFilter(metadataField, normalizedDataType, @operator, null, null));
        }

        if (errors.Count > 0)
        {
            throw new ReportValidationException(errors);
        }

        return validatedFilters;
    }

    private static string BuildSelectClause(
        IReadOnlyList<DatasetField> selectedFields,
        IReadOnlyList<ValidatedGrouping> grouping,
        IReadOnlyList<ValidatedSummary> summaries)
    {
        if (summaries.Count == 0)
        {
            var selectedColumns = selectedFields.Select(field => QuoteIdentifierIfNeeded(field.FieldName));
            return $"SELECT {string.Join(", ", selectedColumns)}";
        }

        var groupingColumns = grouping
            .OrderBy(group => group.GroupOrder)
            .Select(group => QuoteIdentifierIfNeeded(group.Field.FieldName));

        var summaryColumns = BuildSummarySelectClause(summaries);
        var selectColumns = groupingColumns.Concat(summaryColumns);

        return $"SELECT {string.Join(", ", selectColumns)}";
    }

    private static IReadOnlyList<string> BuildSummarySelectClause(IReadOnlyList<ValidatedSummary> summaries)
    {
        return summaries
            .OrderBy(summary => summary.SummaryOrder)
            .Select(summary =>
            {
                var quotedField = QuoteIdentifierIfNeeded(summary.Field.FieldName);
                var quotedAlias = QuoteIdentifierIfNeeded(summary.Alias);

                return summary.Aggregation switch
                {
                    "count" => $"COUNT({quotedField}) AS {quotedAlias}",
                    "sum" => $"SUM({quotedField}) AS {quotedAlias}",
                    "avg" => $"AVG({quotedField}) AS {quotedAlias}",
                    "min" => $"MIN({quotedField}) AS {quotedAlias}",
                    "max" => $"MAX({quotedField}) AS {quotedAlias}",
                    _ => throw new ReportValidationException(
                        $"Unsupported aggregation '{summary.Aggregation}' for summary field '{summary.Field.FieldName}'.")
                };
            })
            .ToList();
    }

    private static string BuildGroupByClause(IReadOnlyList<ValidatedGrouping> grouping)
    {
        if (grouping.Count == 0)
        {
            return string.Empty;
        }

        var groupByColumns = grouping
            .OrderBy(group => group.GroupOrder)
            .Select(group => QuoteIdentifierIfNeeded(group.Field.FieldName));

        return $"GROUP BY {string.Join(", ", groupByColumns)}";
    }

    private static string BuildOrderByClause(
        IReadOnlyList<DatasetField> selectedFields,
        IReadOnlyList<ValidatedGrouping> grouping,
        IReadOnlyList<ValidatedSummary> summaries)
    {
        if (grouping.Count > 0)
        {
            var orderedGroups = grouping
                .OrderBy(group => group.GroupOrder)
                .Select(group => $"{QuoteIdentifierIfNeeded(group.Field.FieldName)} {group.SortDirection.ToUpperInvariant()}");

            return $"ORDER BY {string.Join(", ", orderedGroups)}";
        }

        if (summaries.Count > 0)
        {
            throw new ReportValidationException("Summaries currently require at least one grouping field.");
        }

        return $"ORDER BY {QuoteIdentifierIfNeeded(selectedFields[0].FieldName)}";
    }

    private string BuildWhereClause(IReadOnlyList<ValidatedFilter> filters, Dictionary<string, object?> parameters)
    {
        if (filters.Count == 0)
        {
            return string.Empty;
        }

        var conditions = new List<string>();
        var parameterIndex = 0;

        foreach (var filter in filters)
        {
            conditions.Add(BuildFilterCondition(filter, parameters, ref parameterIndex));
        }

        return $"WHERE {string.Join($"{Environment.NewLine}AND ", conditions)}";
    }

    private string BuildFilterCondition(ValidatedFilter filter, Dictionary<string, object?> parameters, ref int parameterIndex)
    {
        var quotedField = QuoteIdentifierIfNeeded(filter.Field.FieldName);
        var @operator = filter.Operator;

        if (filter.NormalizedDataType.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            return @operator switch
            {
                "equals" => $"{quotedField} = {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "notEquals" => $"{quotedField} <> {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "contains" => $"{quotedField} LIKE {CreateParameter(parameters, $"%{filter.PrimaryValue}%", ref parameterIndex)}",
                "startsWith" => $"{quotedField} LIKE {CreateParameter(parameters, $"{filter.PrimaryValue}%", ref parameterIndex)}",
                "endsWith" => $"{quotedField} LIKE {CreateParameter(parameters, $"%{filter.PrimaryValue}", ref parameterIndex)}",
                "isBlank" => $"({quotedField} IS NULL OR LTRIM(RTRIM({quotedField})) = '')",
                "isNotBlank" => $"({quotedField} IS NOT NULL AND LTRIM(RTRIM({quotedField})) <> '')",
                _ => throw new ReportValidationException($"Unsupported operator '{@operator}' for string field '{filter.Field.FieldName}'.")
            };
        }

        if (filter.NormalizedDataType.Equals("number", StringComparison.OrdinalIgnoreCase))
        {
            return @operator switch
            {
                "equals" => $"{quotedField} = {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "notEquals" => $"{quotedField} <> {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "greaterThan" => $"{quotedField} > {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "greaterThanOrEqual" => $"{quotedField} >= {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "lessThan" => $"{quotedField} < {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "lessThanOrEqual" => $"{quotedField} <= {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "between" => $"{quotedField} BETWEEN {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)} AND {CreateParameter(parameters, filter.SecondaryValue, ref parameterIndex)}",
                "isNull" => $"{quotedField} IS NULL",
                "isNotNull" => $"{quotedField} IS NOT NULL",
                _ => throw new ReportValidationException($"Unsupported operator '{@operator}' for number field '{filter.Field.FieldName}'.")
            };
        }

        if (filter.NormalizedDataType.Equals("date", StringComparison.OrdinalIgnoreCase))
        {
            return @operator switch
            {
                "equals" => $"CAST({quotedField} AS date) = {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "before" => $"{quotedField} < {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "after" => $"{quotedField} > {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                "between" => $"{quotedField} BETWEEN {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)} AND {CreateParameter(parameters, filter.SecondaryValue, ref parameterIndex)}",
                "isNull" => $"{quotedField} IS NULL",
                "isNotNull" => $"{quotedField} IS NOT NULL",
                _ => throw new ReportValidationException($"Unsupported operator '{@operator}' for date field '{filter.Field.FieldName}'.")
            };
        }

        if (filter.NormalizedDataType.Equals("boolean", StringComparison.OrdinalIgnoreCase))
        {
            return @operator switch
            {
                "equals" => $"{quotedField} = {CreateParameter(parameters, filter.PrimaryValue, ref parameterIndex)}",
                _ => throw new ReportValidationException($"Unsupported operator '{@operator}' for boolean field '{filter.Field.FieldName}'.")
            };
        }

        throw new ReportValidationException($"Unsupported data type '{filter.NormalizedDataType}' for field '{filter.Field.FieldName}'.");
    }

    private static string CreateParameter(IDictionary<string, object?> parameters, object? value, ref int parameterIndex)
    {
        var parameterName = $"@p{parameterIndex++}";
        parameters[parameterName] = value;
        return parameterName;
    }

    private static bool ValidateOperatorForDataType(string normalizedDataType, string @operator)
    {
        return AllowedOperatorsByDataType.TryGetValue(normalizedDataType, out var allowedOperators) &&
               allowedOperators.Contains(@operator);
    }

    private static bool ValidateAggregationForType(string normalizedDataType, string aggregation)
    {
        return AllowedAggregationsByDataType.TryGetValue(normalizedDataType, out var allowedAggregations) &&
               allowedAggregations.Contains(aggregation);
    }

    private static bool RequiresValue(string @operator)
    {
        return !OperatorsWithoutValues.Contains(@operator);
    }

    private static List<GroupDefinitionDto> NormalizeGrouping(IReadOnlyList<GroupDefinitionDto> grouping)
    {
        return grouping
            .Select((group, index) => new
            {
                Group = group,
                Index = index,
                Order = group.GroupOrder <= 0 ? index + 1 : group.GroupOrder
            })
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Index)
            .Select(item => item.Group)
            .ToList();
    }

    private static List<SummaryDefinitionDto> NormalizeSummaries(IReadOnlyList<SummaryDefinitionDto> summaries)
    {
        return summaries
            .Select((summary, index) => new
            {
                Summary = summary,
                Index = index,
                Order = summary.SummaryOrder <= 0 ? index + 1 : summary.SummaryOrder
            })
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Index)
            .Select(item => item.Summary)
            .ToList();
    }

    private static string? NormalizeSortDirection(string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortDirection))
        {
            return null;
        }

        return sortDirection.Trim().ToLowerInvariant() switch
        {
            "asc" => "asc",
            "desc" => "desc",
            _ => null
        };
    }

    private static string NormalizeDataType(string rawDataType)
    {
        var normalized = (rawDataType ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Contains("bool"))
        {
            return "boolean";
        }

        if (normalized.Contains("date") || normalized.Contains("time"))
        {
            return "date";
        }

        var numberTokens = new[] { "number", "numeric", "int", "decimal", "double", "float", "long", "short" };
        if (numberTokens.Any(token => normalized.Contains(token)))
        {
            return "number";
        }

        return "string";
    }

    private static bool TryNormalizeValue(
        object? rawValue,
        string normalizedDataType,
        string @operator,
        out object? primaryValue,
        out object? secondaryValue,
        out string error)
    {
        primaryValue = null;
        secondaryValue = null;
        error = string.Empty;

        if (@operator.Equals("between", StringComparison.OrdinalIgnoreCase))
        {
            if (normalizedDataType.Equals("number", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryNormalizeBetweenNumbers(rawValue, out var minValue, out var maxValue, out error))
                {
                    return false;
                }

                primaryValue = minValue;
                secondaryValue = maxValue;
                return true;
            }

            if (normalizedDataType.Equals("date", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryNormalizeBetweenDates(rawValue, out var startDate, out var endDate, out error))
                {
                    return false;
                }

                primaryValue = startDate;
                secondaryValue = endDate;
                return true;
            }

            error = $"The 'between' operator is not supported for data type '{normalizedDataType}'.";
            return false;
        }

        if (normalizedDataType.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryReadString(rawValue, out var stringValue))
            {
                error = "Expected a string value.";
                return false;
            }

            primaryValue = stringValue;
            return true;
        }

        if (normalizedDataType.Equals("number", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryReadDecimal(rawValue, out var numericValue))
            {
                error = "Expected a numeric value.";
                return false;
            }

            primaryValue = numericValue;
            return true;
        }

        if (normalizedDataType.Equals("date", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryReadDate(rawValue, out var dateValue))
            {
                error = "Expected a date value in 'yyyy-MM-dd' or ISO format.";
                return false;
            }

            primaryValue = dateValue;
            return true;
        }

        if (normalizedDataType.Equals("boolean", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryReadBoolean(rawValue, out var booleanValue))
            {
                error = "Expected a boolean value.";
                return false;
            }

            primaryValue = booleanValue;
            return true;
        }

        error = $"Unsupported data type '{normalizedDataType}'.";
        return false;
    }

    private static bool TryNormalizeBetweenNumbers(object? rawValue, out decimal minValue, out decimal maxValue, out string error)
    {
        minValue = default;
        maxValue = default;
        error = string.Empty;

        if (!TryReadBetweenMembers(rawValue, out var first, out var second, out error))
        {
            return false;
        }

        if (!TryReadDecimal(first, out minValue) || !TryReadDecimal(second, out maxValue))
        {
            error = "Between number operator expects both min and max numeric values.";
            return false;
        }

        return true;
    }

    private static bool TryNormalizeBetweenDates(object? rawValue, out DateOnly startDate, out DateOnly endDate, out string error)
    {
        startDate = default;
        endDate = default;
        error = string.Empty;

        if (!TryReadBetweenMembers(rawValue, out var first, out var second, out error))
        {
            return false;
        }

        if (!TryReadDate(first, out startDate) || !TryReadDate(second, out endDate))
        {
            error = "Between date operator expects both start and end date values.";
            return false;
        }

        return true;
    }

    private static bool TryReadBetweenMembers(object? rawValue, out object? first, out object? second, out string error)
    {
        first = null;
        second = null;
        error = string.Empty;

        if (rawValue is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                if (TryGetObjectProperty(jsonElement, ["min", "start"], out first) &&
                    TryGetObjectProperty(jsonElement, ["max", "end"], out second))
                {
                    return true;
                }

                error = "Between value must include either min/max or start/end.";
                return false;
            }

            if (jsonElement.ValueKind == JsonValueKind.Array && jsonElement.GetArrayLength() == 2)
            {
                first = jsonElement[0];
                second = jsonElement[1];
                return true;
            }
        }

        if (rawValue is IDictionary<string, object?> dictionary)
        {
            if (TryGetDictionaryProperty(dictionary, ["min", "start"], out first) &&
                TryGetDictionaryProperty(dictionary, ["max", "end"], out second))
            {
                return true;
            }
        }

        if (rawValue is IReadOnlyList<object?> objectList && objectList.Count == 2)
        {
            first = objectList[0];
            second = objectList[1];
            return true;
        }

        if (rawValue is object?[] objectArray && objectArray.Length == 2)
        {
            first = objectArray[0];
            second = objectArray[1];
            return true;
        }

        error = "Between value must be an object with min/max (or start/end), or an array with two values.";
        return false;
    }

    private static bool TryGetObjectProperty(JsonElement element, IEnumerable<string> candidateNames, out object? value)
    {
        foreach (var candidateName in candidateNames)
        {
            if (element.TryGetProperty(candidateName, out var property))
            {
                value = property;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetDictionaryProperty(IDictionary<string, object?> source, IEnumerable<string> candidateNames, out object? value)
    {
        foreach (var candidateName in candidateNames)
        {
            if (source.TryGetValue(candidateName, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryReadString(object? rawValue, out string value)
    {
        value = string.Empty;

        if (rawValue is null)
        {
            return false;
        }

        if (rawValue is string str)
        {
            value = str;
            return true;
        }

        if (rawValue is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = jsonElement.GetString() ?? string.Empty;
            return true;
        }

        return false;
    }

    private static bool TryReadDecimal(object? rawValue, out decimal value)
    {
        value = default;

        if (rawValue is null)
        {
            return false;
        }

        if (rawValue is decimal decimalValue)
        {
            value = decimalValue;
            return true;
        }

        if (rawValue is double doubleValue)
        {
            value = Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
            return true;
        }

        if (rawValue is float floatValue)
        {
            value = Convert.ToDecimal(floatValue, CultureInfo.InvariantCulture);
            return true;
        }

        if (rawValue is int intValue)
        {
            value = intValue;
            return true;
        }

        if (rawValue is long longValue)
        {
            value = longValue;
            return true;
        }

        if (rawValue is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetDecimal(out value))
            {
                return true;
            }

            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                return decimal.TryParse(
                    jsonElement.GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out value);
            }

            return false;
        }

        if (rawValue is string stringValue)
        {
            return decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        return decimal.TryParse(Convert.ToString(rawValue, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadDate(object? rawValue, out DateOnly value)
    {
        value = default;

        if (rawValue is null)
        {
            return false;
        }

        if (rawValue is DateOnly dateOnlyValue)
        {
            value = dateOnlyValue;
            return true;
        }

        if (rawValue is DateTime dateTimeValue)
        {
            value = DateOnly.FromDateTime(dateTimeValue);
            return true;
        }

        if (rawValue is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            return TryParseDateString(jsonElement.GetString(), out value);
        }

        if (rawValue is string stringValue)
        {
            return TryParseDateString(stringValue, out value);
        }

        return false;
    }

    private static bool TryParseDateString(string? input, out DateOnly value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            return true;
        }

        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            value = DateOnly.FromDateTime(dateTime);
            return true;
        }

        return false;
    }

    private static bool TryReadBoolean(object? rawValue, out bool value)
    {
        value = default;

        if (rawValue is null)
        {
            return false;
        }

        if (rawValue is bool boolValue)
        {
            value = boolValue;
            return true;
        }

        if (rawValue is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False)
            {
                value = jsonElement.GetBoolean();
                return true;
            }

            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                return bool.TryParse(jsonElement.GetString(), out value);
            }

            return false;
        }

        if (rawValue is string stringValue)
        {
            return bool.TryParse(stringValue, out value);
        }

        return false;
    }

    private static string QuoteIdentifierIfNeeded(string identifier, bool allowPath = false)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ReportValidationException("SQL identifier cannot be empty.");
        }

        if (allowPath)
        {
            if (!SqlIdentifierPathPattern.IsMatch(identifier))
            {
                throw new ReportValidationException($"Invalid SQL identifier path '{identifier}'.");
            }

            var parts = identifier.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return string.Join(".", parts.Select(part => $"[{part.Replace("]", "]]", StringComparison.Ordinal)}]"));
        }

        if (!SqlIdentifierPattern.IsMatch(identifier))
        {
            throw new ReportValidationException($"Invalid SQL identifier '{identifier}'.");
        }

        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    private sealed record ValidatedFilter(
        DatasetField Field,
        string NormalizedDataType,
        string Operator,
        object? PrimaryValue,
        object? SecondaryValue);

    private sealed record ValidatedGrouping(
        DatasetField Field,
        string SortDirection,
        int GroupOrder);

    private sealed record ValidatedSummary(
        DatasetField Field,
        string Aggregation,
        string Alias,
        int SummaryOrder);
}
