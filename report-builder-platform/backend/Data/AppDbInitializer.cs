using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public static class AppDbInitializer
{
    private static readonly Guid PermitsDatasetId = Guid.Parse("1F721EB3-3AB9-4A8F-BCFB-C41B10B13A10");
    private static readonly Guid InspectionsDatasetId = Guid.Parse("EEB16A39-0E8B-4F72-B571-C7F5835ACE16");

    public static async Task SeedMetadataAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var createdAt = DateTime.UtcNow;

        var permitsDataset = await dbContext.Datasets
            .FirstOrDefaultAsync(dataset => dataset.Name == "Permits", cancellationToken);
        if (permitsDataset is null)
        {
            permitsDataset = new Dataset
            {
                Id = PermitsDatasetId,
                Name = "Permits",
                Description = "Permit reporting dataset",
                ViewName = "vw_permits_reporting",
                CreatedAt = createdAt
            };

            await dbContext.Datasets.AddAsync(permitsDataset, cancellationToken);
        }

        var inspectionsDataset = await dbContext.Datasets
            .FirstOrDefaultAsync(dataset => dataset.Name == "Inspections", cancellationToken);
        if (inspectionsDataset is null)
        {
            inspectionsDataset = new Dataset
            {
                Id = InspectionsDatasetId,
                Name = "Inspections",
                Description = "Inspections reporting dataset",
                ViewName = "vw_inspections_reporting",
                CreatedAt = createdAt
            };

            await dbContext.Datasets.AddAsync(inspectionsDataset, cancellationToken);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await SeedFieldsForDatasetAsync(
            dbContext,
            permitsDataset.Id,
            new List<DatasetField>
            {
                new()
                {
                    Id = Guid.Parse("AA67A03E-3215-43B0-B7B2-C1196A1FDF3D"),
                    DatasetId = permitsDataset.Id,
                    FieldName = "PermitNumber",
                    DisplayName = "Permit Number",
                    DataType = "string",
                    IsFilterable = true,
                    IsGroupable = true,
                    IsSummarizable = false
                },
                new()
                {
                    Id = Guid.Parse("2B03E6EB-CB43-4A12-AB12-35CBB4FD3356"),
                    DatasetId = permitsDataset.Id,
                    FieldName = "ApplicantName",
                    DisplayName = "Applicant Name",
                    DataType = "string",
                    IsFilterable = true,
                    IsGroupable = true,
                    IsSummarizable = false
                },
                new()
                {
                    Id = Guid.Parse("FFCF7B94-B887-44FD-A565-FE9BA4D09F01"),
                    DatasetId = permitsDataset.Id,
                    FieldName = "PermitType",
                    DisplayName = "Permit Type",
                    DataType = "string",
                    IsFilterable = true,
                    IsGroupable = true,
                    IsSummarizable = false
                },
                new()
                {
                    Id = Guid.Parse("9966DFD1-36B0-4117-AD98-8F24A4F290E0"),
                    DatasetId = permitsDataset.Id,
                    FieldName = "IssueDate",
                    DisplayName = "Issue Date",
                    DataType = "date",
                    IsFilterable = true,
                    IsGroupable = true,
                    IsSummarizable = false
                },
                new()
                {
                    Id = Guid.Parse("C66E0A02-D74F-487B-BB54-BF8DAB3B3DE8"),
                    DatasetId = permitsDataset.Id,
                    FieldName = "Status",
                    DisplayName = "Status",
                    DataType = "string",
                    IsFilterable = true,
                    IsGroupable = true,
                    IsSummarizable = false
                }
            },
            cancellationToken);

        await SeedFieldsForDatasetAsync(
            dbContext,
            inspectionsDataset.Id,
            new List<DatasetField>
            {
                new()
                {
                    Id = Guid.Parse("89F0E0DE-602F-4E58-9126-A7199F5FD8E7"),
                    DatasetId = inspectionsDataset.Id,
                    FieldName = "InspectionId",
                    DisplayName = "Inspection ID",
                    DataType = "string",
                    IsFilterable = true,
                    IsGroupable = false,
                    IsSummarizable = false
                },
                new()
                {
                    Id = Guid.Parse("2EC732C8-0B9A-439F-A46D-1B4D78A6E75B"),
                    DatasetId = inspectionsDataset.Id,
                    FieldName = "InspectorName",
                    DisplayName = "Inspector Name",
                    DataType = "string",
                    IsFilterable = true,
                    IsGroupable = true,
                    IsSummarizable = false
                },
                new()
                {
                    Id = Guid.Parse("AA3BC71E-0995-42B0-B4FA-B3F8956F3361"),
                    DatasetId = inspectionsDataset.Id,
                    FieldName = "InspectionDate",
                    DisplayName = "Inspection Date",
                    DataType = "date",
                    IsFilterable = true,
                    IsGroupable = true,
                    IsSummarizable = false
                },
                new()
                {
                    Id = Guid.Parse("BA58FE6B-BD2F-47E4-A4E1-02B5A23A6E9E"),
                    DatasetId = inspectionsDataset.Id,
                    FieldName = "InspectionStatus",
                    DisplayName = "Inspection Status",
                    DataType = "string",
                    IsFilterable = true,
                    IsGroupable = true,
                    IsSummarizable = false
                }
            },
            cancellationToken);

        await SeedPreviewViewsAndSampleRowsAsync(dbContext, cancellationToken);
    }

    private static async Task SeedFieldsForDatasetAsync(
        AppDbContext dbContext,
        Guid datasetId,
        IEnumerable<DatasetField> seededFields,
        CancellationToken cancellationToken)
    {
        var existingFieldNames = await dbContext.DatasetFields
            .AsNoTracking()
            .Where(field => field.DatasetId == datasetId)
            .Select(field => field.FieldName)
            .ToListAsync(cancellationToken);

        var missingFields = seededFields
            .Where(field => !existingFieldNames.Contains(field.FieldName, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingFields.Count == 0)
        {
            return;
        }

        await dbContext.DatasetFields.AddRangeAsync(missingFields, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPreviewViewsAndSampleRowsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        const string createPermitsTableSql = """
            IF OBJECT_ID('dbo.PermitsPreviewData', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.PermitsPreviewData
                (
                    PermitNumber NVARCHAR(50) NOT NULL,
                    ApplicantName NVARCHAR(150) NOT NULL,
                    PermitType NVARCHAR(100) NOT NULL,
                    IssueDate DATE NOT NULL,
                    Status NVARCHAR(50) NOT NULL
                );
            END
            """;

        const string createInspectionsTableSql = """
            IF OBJECT_ID('dbo.InspectionsPreviewData', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.InspectionsPreviewData
                (
                    InspectionId NVARCHAR(50) NOT NULL,
                    InspectorName NVARCHAR(150) NOT NULL,
                    InspectionDate DATE NOT NULL,
                    InspectionStatus NVARCHAR(50) NOT NULL
                );
            END
            """;

        const string seedPermitsRowsSql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.PermitsPreviewData)
            BEGIN
                INSERT INTO dbo.PermitsPreviewData (PermitNumber, ApplicantName, PermitType, IssueDate, Status)
                VALUES
                    ('PR-1001', 'Jane Doe', 'Residential', '2024-01-05', 'Active'),
                    ('PR-1002', 'Acme Builders', 'Commercial', '2024-01-12', 'Pending'),
                    ('PR-1003', 'Northside Holdings', 'Signage', '2024-02-01', 'Active');
            END
            """;

        const string seedInspectionsRowsSql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.InspectionsPreviewData)
            BEGIN
                INSERT INTO dbo.InspectionsPreviewData (InspectionId, InspectorName, InspectionDate, InspectionStatus)
                VALUES
                    ('INSP-3001', 'Luis Martinez', '2024-01-06', 'Passed'),
                    ('INSP-3002', 'Ava Johnson', '2024-01-17', 'Failed'),
                    ('INSP-3003', 'Elena Singh', '2024-02-08', 'Passed');
            END
            """;

        const string createPermitsViewSql = """
            CREATE OR ALTER VIEW dbo.vw_permits_reporting
            AS
            SELECT
                PermitNumber,
                ApplicantName,
                PermitType,
                IssueDate,
                Status
            FROM dbo.PermitsPreviewData;
            """;

        const string createInspectionsViewSql = """
            CREATE OR ALTER VIEW dbo.vw_inspections_reporting
            AS
            SELECT
                InspectionId,
                InspectorName,
                InspectionDate,
                InspectionStatus
            FROM dbo.InspectionsPreviewData;
            """;

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(createPermitsTableSql, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(createInspectionsTableSql, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(seedPermitsRowsSql, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(seedInspectionsRowsSql, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(createPermitsViewSql, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(createInspectionsViewSql, cancellationToken);
        }
        catch
        {
            // Preview fixture objects are optional and should not block startup in locked-down environments.
        }
    }
}
