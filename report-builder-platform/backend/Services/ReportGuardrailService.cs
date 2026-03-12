using System.Globalization;
using System.Text.Json;
using backend.DTOs;
using backend.Models;
using Microsoft.Extensions.Options;

namespace backend.Services;

public class ReportGuardrailService(IOptions<ReportingOptions> reportingOptionsAccessor) : IReportGuardrailService
{
    private static readonly HashSet<string> OperatorsWithoutValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "isBlank",
        "isNotBlank",
        "isNull",
        "isNotNull"
    };

    private readonly ReportingOptions _reportingOptions = reportingOptionsAccessor.Value;

    public GuardrailExecutionSettings ValidatePreviewRequest(
        ReportDefinitionDto definition,
        Dataset dataset,
        IReadOnlyDictionary<string, DatasetField> datasetFieldMap)
    {
        var errors = ValidateCommonRules(definition, dataset, datasetFieldMap);
        if (errors.Count > 0)
        {
            throw new ReportValidationException(errors);
        }

        return new GuardrailExecutionSettings
        {
            PreviewRowLimit = ResolvePreviewRowLimit(dataset),
            MaxExecutionRowLimit = ResolveExecutionRowLimit(dataset),
            TimeoutSeconds = ResolveTimeoutSeconds(dataset)
        };
    }

    public GuardrailExecutionSettings ValidateExecutionRequest(
        ReportDefinitionDto definition,
        Dataset dataset,
        IReadOnlyDictionary<string, DatasetField> datasetFieldMap)
    {
        var errors = ValidateCommonRules(definition, dataset, datasetFieldMap);
        if (errors.Count > 0)
        {
            throw new ReportValidationException(errors);
        }

        return new GuardrailExecutionSettings
        {
            PreviewRowLimit = ResolvePreviewRowLimit(dataset),
            MaxExecutionRowLimit = ResolveExecutionRowLimit(dataset),
            TimeoutSeconds = ResolveTimeoutSeconds(dataset)
        };
    }

    public bool HasAnyFilters(ReportDefinitionDto definition)
    {
        return (definition.Filters ?? []).Any(filter =>
            !string.IsNullOrWhiteSpace(filter.FieldName) &&
            !string.IsNullOrWhiteSpace(filter.Operator));
    }

    public bool HasDateFilter(ReportDefinitionDto definition, IReadOnlyDictionary<string, DatasetField> datasetFieldMap)
    {
        foreach (var filter in definition.Filters ?? [])
        {
            var fieldName = filter.FieldName?.Trim();
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                continue;
            }

            if (!datasetFieldMap.TryGetValue(fieldName, out var datasetField))
            {
                continue;
            }

            if (!NormalizeDataType(datasetField.DataType).Equals("date", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var @operator = filter.Operator?.Trim();
            if (string.IsNullOrWhiteSpace(@operator))
            {
                continue;
            }

            if (IsValidDateFilterValue(@operator, filter.Value))
            {
                return true;
            }
        }

        return false;
    }

    public int ResolvePreviewRowLimit(Dataset dataset)
    {
        var value = dataset.PreviewRowLimit ?? _reportingOptions.DefaultPreviewRowLimit;
        return value > 0 ? value : _reportingOptions.DefaultPreviewRowLimit;
    }

    public int ResolveExecutionRowLimit(Dataset dataset)
    {
        var value = dataset.MaxExecutionRowLimit ?? _reportingOptions.DefaultMaxExecutionRowLimit;
        return value > 0 ? value : _reportingOptions.DefaultMaxExecutionRowLimit;
    }

    public int ResolveTimeoutSeconds(Dataset dataset)
    {
        var value = dataset.TimeoutSeconds ?? _reportingOptions.DefaultTimeoutSeconds;
        return value > 0 ? value : _reportingOptions.DefaultTimeoutSeconds;
    }

    private List<string> ValidateCommonRules(
        ReportDefinitionDto definition,
        Dataset dataset,
        IReadOnlyDictionary<string, DatasetField> datasetFieldMap)
    {
        var errors = new List<string>();

        if ((definition.Fields?.Count ?? 0) == 0)
        {
            errors.Add("At least one selected field is required.");
        }

        if (dataset.RequireAtLeastOneFilter && !HasAnyFilters(definition))
        {
            errors.Add($"At least one filter is required for the {dataset.Name} dataset.");
        }

        if (dataset.RequireDateFilter && !HasDateFilter(definition, datasetFieldMap))
        {
            errors.Add($"A date filter is required for the {dataset.Name} dataset.");
        }

        return errors;
    }

    private static bool IsValidDateFilterValue(string @operator, object? value)
    {
        if (OperatorsWithoutValues.Contains(@operator))
        {
            return true;
        }

        if (@operator.Equals("between", StringComparison.OrdinalIgnoreCase))
        {
            return TryGetBetweenDateValues(value, out _);
        }

        return TryReadDate(value, out _);
    }

    private static bool TryGetBetweenDateValues(object? rawValue, out (DateOnly Start, DateOnly End) value)
    {
        value = default;

        if (rawValue is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                if (!TryGetObjectProperty(jsonElement, ["start", "min"], out var startElement) ||
                    !TryGetObjectProperty(jsonElement, ["end", "max"], out var endElement))
                {
                    return false;
                }

                if (!TryReadDate(startElement, out var startDate) || !TryReadDate(endElement, out var endDate))
                {
                    return false;
                }

                value = (startDate, endDate);
                return true;
            }

            if (jsonElement.ValueKind == JsonValueKind.Array && jsonElement.GetArrayLength() == 2)
            {
                if (!TryReadDate(jsonElement[0], out var startDate) || !TryReadDate(jsonElement[1], out var endDate))
                {
                    return false;
                }

                value = (startDate, endDate);
                return true;
            }
        }

        if (rawValue is IDictionary<string, object?> dictionary)
        {
            if (!TryGetDictionaryProperty(dictionary, ["start", "min"], out var startRaw) ||
                !TryGetDictionaryProperty(dictionary, ["end", "max"], out var endRaw))
            {
                return false;
            }

            if (!TryReadDate(startRaw, out var startDate) || !TryReadDate(endRaw, out var endDate))
            {
                return false;
            }

            value = (startDate, endDate);
            return true;
        }

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

    private static bool TryGetDictionaryProperty(
        IDictionary<string, object?> source,
        IEnumerable<string> candidateNames,
        out object? value)
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

    private static bool TryReadDate(object? rawValue, out DateOnly value)
    {
        value = default;

        if (rawValue is null)
        {
            return false;
        }

        if (rawValue is DateOnly dateOnly)
        {
            value = dateOnly;
            return true;
        }

        if (rawValue is DateTime dateTime)
        {
            value = DateOnly.FromDateTime(dateTime);
            return true;
        }

        if (rawValue is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            return TryParseDate(jsonElement.GetString(), out value);
        }

        if (rawValue is string stringValue)
        {
            return TryParseDate(stringValue, out value);
        }

        return false;
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            date = DateOnly.FromDateTime(dateTime);
            return true;
        }

        return false;
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
}
