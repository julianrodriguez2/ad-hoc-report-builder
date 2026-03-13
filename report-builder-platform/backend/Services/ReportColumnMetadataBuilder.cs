using backend.DTOs;
using backend.Models;

namespace backend.Services;

internal static class ReportColumnMetadataBuilder
{
    public static List<PreviewColumnDto> BuildColumns(
        ReportDefinitionDto definition,
        IReadOnlyDictionary<string, DatasetField> datasetFieldMap)
    {
        var columns = new List<PreviewColumnDto>();

        if ((definition.Summaries?.Count ?? 0) > 0)
        {
            var orderedGrouping = (definition.Grouping ?? [])
                .Select((group, index) => new
                {
                    Group = group,
                    Order = group.GroupOrder <= 0 ? index + 1 : group.GroupOrder,
                    Index = index
                })
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Index)
                .Select(item => item.Group);

            foreach (var group in orderedGrouping)
            {
                var fieldName = group.FieldName?.Trim();
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

            var orderedSummaries = (definition.Summaries ?? [])
                .Select((summary, index) => new
                {
                    Summary = summary,
                    Order = summary.SummaryOrder <= 0 ? index + 1 : summary.SummaryOrder,
                    Index = index
                })
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Index)
                .Select(item => item.Summary);

            foreach (var summary in orderedSummaries)
            {
                var alias = summary.Alias?.Trim();
                if (string.IsNullOrWhiteSpace(alias))
                {
                    continue;
                }

                columns.Add(new PreviewColumnDto
                {
                    FieldName = alias,
                    DisplayName = alias
                });
            }

            return columns;
        }

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
}
