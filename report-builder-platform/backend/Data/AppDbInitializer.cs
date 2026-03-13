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
        await EnsureDatasetGuardrailColumnsAsync(dbContext, cancellationToken);
        await EnsureSavedReportsTableSchemaAsync(dbContext, cancellationToken);

        var createdAt = DateTime.UtcNow;
        var hasDatasetUpdates = false;

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
                CreatedAt = createdAt,
                PreviewRowLimit = 100,
                MaxExecutionRowLimit = 10000,
                RequireAtLeastOneFilter = false,
                RequireDateFilter = false,
                LargeDatasetThreshold = 50000,
                TimeoutSeconds = 10
            };

            await dbContext.Datasets.AddAsync(permitsDataset, cancellationToken);
            hasDatasetUpdates = true;
        }
        else
        {
            hasDatasetUpdates |= ApplyDatasetGuardrails(
                permitsDataset,
                previewRowLimit: 100,
                maxExecutionRowLimit: 10000,
                requireAtLeastOneFilter: false,
                requireDateFilter: false,
                largeDatasetThreshold: 50000,
                timeoutSeconds: 10);
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
                CreatedAt = createdAt,
                PreviewRowLimit = 100,
                MaxExecutionRowLimit = 5000,
                RequireAtLeastOneFilter = true,
                RequireDateFilter = true,
                LargeDatasetThreshold = 25000,
                TimeoutSeconds = 10
            };

            await dbContext.Datasets.AddAsync(inspectionsDataset, cancellationToken);
            hasDatasetUpdates = true;
        }
        else
        {
            hasDatasetUpdates |= ApplyDatasetGuardrails(
                inspectionsDataset,
                previewRowLimit: 100,
                maxExecutionRowLimit: 5000,
                requireAtLeastOneFilter: true,
                requireDateFilter: true,
                largeDatasetThreshold: 25000,
                timeoutSeconds: 10);
        }

        if (hasDatasetUpdates || dbContext.ChangeTracker.HasChanges())
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
                    IsSummarizable = true
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
                    IsSummarizable = true
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
                    IsSummarizable = true
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
                    IsSummarizable = true
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
                    IsSummarizable = true
                },
                new()
                {
                    Id = Guid.Parse("FBD5B3B4-F9D4-4B32-BCC2-56A1D65C31D7"),
                    DatasetId = permitsDataset.Id,
                    FieldName = "FeeAmount",
                    DisplayName = "Fee Amount",
                    DataType = "number",
                    IsFilterable = true,
                    IsGroupable = false,
                    IsSummarizable = true
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
                    IsSummarizable = true
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
                    IsSummarizable = true
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
                    IsSummarizable = true
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
                    IsSummarizable = true
                }
            },
            cancellationToken);

        await SeedPreviewViewsAndSampleRowsAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureSavedReportsTableSchemaAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        const string ensureSavedReportsTableSql = """
            IF OBJECT_ID('dbo.SavedReports', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.SavedReports
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SavedReports PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Description NVARCHAR(1000) NULL,
                    DatasetId UNIQUEIDENTIFIER NOT NULL,
                    DefinitionJson NVARCHAR(MAX) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(256) NULL
                );

                CREATE INDEX IX_SavedReports_DatasetId ON dbo.SavedReports (DatasetId);
            END
            """;

        const string rebuildLegacySavedReportsTableSql = """
            IF OBJECT_ID('dbo.SavedReports', 'U') IS NOT NULL
               AND EXISTS
               (
                   SELECT 1
                   FROM sys.columns
                   WHERE object_id = OBJECT_ID('dbo.SavedReports')
                     AND name = 'Id'
                     AND system_type_id <> TYPE_ID('uniqueidentifier')
               )
            BEGIN
                CREATE TABLE dbo.SavedReports_New
                (
                    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SavedReports_New PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Description NVARCHAR(1000) NULL,
                    DatasetId UNIQUEIDENTIFIER NOT NULL,
                    DefinitionJson NVARCHAR(MAX) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    UpdatedAt DATETIME2 NULL,
                    CreatedBy NVARCHAR(256) NULL
                );

                INSERT INTO dbo.SavedReports_New (Id, Name, Description, DatasetId, DefinitionJson, CreatedAt, UpdatedAt, CreatedBy)
                SELECT
                    NEWID(),
                    ISNULL(NULLIF(Name, ''), 'Untitled Report'),
                    NULL,
                    ISNULL(
                        TRY_CONVERT(
                            uniqueidentifier,
                            CASE WHEN ISJSON(DefinitionJson) = 1 THEN JSON_VALUE(DefinitionJson, '$.datasetId') END),
                        '00000000-0000-0000-0000-000000000000'),
                    ISNULL(NULLIF(DefinitionJson, ''), '{}'),
                    ISNULL(CreatedAt, SYSUTCDATETIME()),
                    NULL,
                    NULLIF(CreatedBy, '')
                FROM dbo.SavedReports;

                DROP TABLE dbo.SavedReports;
                EXEC sp_rename 'dbo.SavedReports_New', 'SavedReports';
                EXEC sp_rename 'PK_SavedReports_New', 'PK_SavedReports';

                CREATE INDEX IX_SavedReports_DatasetId ON dbo.SavedReports (DatasetId);
            END
            """;

        const string ensureSavedReportsColumnsSql = """
            IF COL_LENGTH('dbo.SavedReports', 'Description') IS NULL
            BEGIN
                ALTER TABLE dbo.SavedReports ADD Description NVARCHAR(1000) NULL;
            END

            IF COL_LENGTH('dbo.SavedReports', 'DatasetId') IS NULL
            BEGIN
                ALTER TABLE dbo.SavedReports ADD DatasetId UNIQUEIDENTIFIER NULL;
                UPDATE dbo.SavedReports
                SET DatasetId = ISNULL(
                    TRY_CONVERT(
                        uniqueidentifier,
                        CASE WHEN ISJSON(DefinitionJson) = 1 THEN JSON_VALUE(DefinitionJson, '$.datasetId') END),
                    '00000000-0000-0000-0000-000000000000');
                ALTER TABLE dbo.SavedReports ALTER COLUMN DatasetId UNIQUEIDENTIFIER NOT NULL;
            END

            IF COL_LENGTH('dbo.SavedReports', 'UpdatedAt') IS NULL
            BEGIN
                ALTER TABLE dbo.SavedReports ADD UpdatedAt DATETIME2 NULL;
            END

            IF COL_LENGTH('dbo.SavedReports', 'CreatedBy') IS NULL
            BEGIN
                ALTER TABLE dbo.SavedReports ADD CreatedBy NVARCHAR(256) NULL;
            END
            """;

        const string ensureSavedReportsIndexSql = """
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE object_id = OBJECT_ID('dbo.SavedReports')
                  AND name = 'IX_SavedReports_DatasetId'
            )
            BEGIN
                CREATE INDEX IX_SavedReports_DatasetId ON dbo.SavedReports (DatasetId);
            END
            """;

        await dbContext.Database.ExecuteSqlRawAsync(ensureSavedReportsTableSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(rebuildLegacySavedReportsTableSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(ensureSavedReportsColumnsSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(ensureSavedReportsIndexSql, cancellationToken);
    }

    private static bool ApplyDatasetGuardrails(
        Dataset dataset,
        int previewRowLimit,
        int maxExecutionRowLimit,
        bool requireAtLeastOneFilter,
        bool requireDateFilter,
        int? largeDatasetThreshold,
        int timeoutSeconds)
    {
        var hasUpdates = false;

        if (dataset.PreviewRowLimit != previewRowLimit)
        {
            dataset.PreviewRowLimit = previewRowLimit;
            hasUpdates = true;
        }

        if (dataset.MaxExecutionRowLimit != maxExecutionRowLimit)
        {
            dataset.MaxExecutionRowLimit = maxExecutionRowLimit;
            hasUpdates = true;
        }

        if (dataset.RequireAtLeastOneFilter != requireAtLeastOneFilter)
        {
            dataset.RequireAtLeastOneFilter = requireAtLeastOneFilter;
            hasUpdates = true;
        }

        if (dataset.RequireDateFilter != requireDateFilter)
        {
            dataset.RequireDateFilter = requireDateFilter;
            hasUpdates = true;
        }

        if (dataset.LargeDatasetThreshold != largeDatasetThreshold)
        {
            dataset.LargeDatasetThreshold = largeDatasetThreshold;
            hasUpdates = true;
        }

        if (dataset.TimeoutSeconds != timeoutSeconds)
        {
            dataset.TimeoutSeconds = timeoutSeconds;
            hasUpdates = true;
        }

        return hasUpdates;
    }

    private static async Task EnsureDatasetGuardrailColumnsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        const string ensurePreviewRowLimitColumnSql = """
            IF COL_LENGTH('dbo.Datasets', 'PreviewRowLimit') IS NULL
            BEGIN
                ALTER TABLE dbo.Datasets ADD PreviewRowLimit INT NULL;
            END
            """;

        const string ensureMaxExecutionRowLimitColumnSql = """
            IF COL_LENGTH('dbo.Datasets', 'MaxExecutionRowLimit') IS NULL
            BEGIN
                ALTER TABLE dbo.Datasets ADD MaxExecutionRowLimit INT NULL;
            END
            """;

        const string ensureRequireAtLeastOneFilterColumnSql = """
            IF COL_LENGTH('dbo.Datasets', 'RequireAtLeastOneFilter') IS NULL
            BEGIN
                ALTER TABLE dbo.Datasets
                ADD RequireAtLeastOneFilter BIT NOT NULL
                    CONSTRAINT DF_Datasets_RequireAtLeastOneFilter DEFAULT(0);
            END
            """;

        const string ensureRequireDateFilterColumnSql = """
            IF COL_LENGTH('dbo.Datasets', 'RequireDateFilter') IS NULL
            BEGIN
                ALTER TABLE dbo.Datasets
                ADD RequireDateFilter BIT NOT NULL
                    CONSTRAINT DF_Datasets_RequireDateFilter DEFAULT(0);
            END
            """;

        const string ensureLargeDatasetThresholdColumnSql = """
            IF COL_LENGTH('dbo.Datasets', 'LargeDatasetThreshold') IS NULL
            BEGIN
                ALTER TABLE dbo.Datasets ADD LargeDatasetThreshold INT NULL;
            END
            """;

        const string ensureTimeoutSecondsColumnSql = """
            IF COL_LENGTH('dbo.Datasets', 'TimeoutSeconds') IS NULL
            BEGIN
                ALTER TABLE dbo.Datasets ADD TimeoutSeconds INT NULL;
            END
            """;

        await dbContext.Database.ExecuteSqlRawAsync(ensurePreviewRowLimitColumnSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(ensureMaxExecutionRowLimitColumnSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(ensureRequireAtLeastOneFilterColumnSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(ensureRequireDateFilterColumnSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(ensureLargeDatasetThresholdColumnSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(ensureTimeoutSecondsColumnSql, cancellationToken);
    }

    private static async Task SeedFieldsForDatasetAsync(
        AppDbContext dbContext,
        Guid datasetId,
        IEnumerable<DatasetField> seededFields,
        CancellationToken cancellationToken)
    {
        var existingFields = await dbContext.DatasetFields
            .Where(field => field.DatasetId == datasetId)
            .ToListAsync(cancellationToken);

        var existingFieldMap = existingFields.ToDictionary(field => field.FieldName, StringComparer.OrdinalIgnoreCase);
        var missingFields = new List<DatasetField>();
        var hasUpdates = false;

        foreach (var seededField in seededFields)
        {
            if (!existingFieldMap.TryGetValue(seededField.FieldName, out var existingField))
            {
                missingFields.Add(seededField);
                continue;
            }

            if (existingField.DisplayName != seededField.DisplayName)
            {
                existingField.DisplayName = seededField.DisplayName;
                hasUpdates = true;
            }

            if (existingField.DataType != seededField.DataType)
            {
                existingField.DataType = seededField.DataType;
                hasUpdates = true;
            }

            if (existingField.IsFilterable != seededField.IsFilterable)
            {
                existingField.IsFilterable = seededField.IsFilterable;
                hasUpdates = true;
            }

            if (existingField.IsGroupable != seededField.IsGroupable)
            {
                existingField.IsGroupable = seededField.IsGroupable;
                hasUpdates = true;
            }

            if (existingField.IsSummarizable != seededField.IsSummarizable)
            {
                existingField.IsSummarizable = seededField.IsSummarizable;
                hasUpdates = true;
            }
        }

        if (missingFields.Count > 0)
        {
            await dbContext.DatasetFields.AddRangeAsync(missingFields, cancellationToken);
            hasUpdates = true;
        }

        if (hasUpdates)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
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
                    Status NVARCHAR(50) NOT NULL,
                    FeeAmount DECIMAL(18,2) NULL
                );
            END
            """;

        const string ensurePermitsFeeAmountColumnSql = """
            IF COL_LENGTH('dbo.PermitsPreviewData', 'FeeAmount') IS NULL
            BEGIN
                ALTER TABLE dbo.PermitsPreviewData
                ADD FeeAmount DECIMAL(18,2) NULL;
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
                INSERT INTO dbo.PermitsPreviewData (PermitNumber, ApplicantName, PermitType, IssueDate, Status, FeeAmount)
                VALUES
                    ('PR-1001', 'Jane Doe', 'Residential', '2024-01-05', 'Active', 250.00),
                    ('PR-1002', 'Acme Builders', 'Commercial', '2024-01-12', 'Pending', 1800.00),
                    ('PR-1003', 'Northside Holdings', 'Signage', '2024-02-01', 'Active', 475.50);
            END
            """;

        const string updatePermitsFeeAmountSql = """
            UPDATE dbo.PermitsPreviewData
            SET FeeAmount = CASE PermitNumber
                WHEN 'PR-1001' THEN 250.00
                WHEN 'PR-1002' THEN 1800.00
                WHEN 'PR-1003' THEN 475.50
                ELSE ISNULL(FeeAmount, 0.00)
            END
            WHERE FeeAmount IS NULL;
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
                Status,
                FeeAmount
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
            await dbContext.Database.ExecuteSqlRawAsync(ensurePermitsFeeAmountColumnSql, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(createInspectionsTableSql, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(seedPermitsRowsSql, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(updatePermitsFeeAmountSql, cancellationToken);
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
