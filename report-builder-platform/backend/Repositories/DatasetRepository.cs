using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class DatasetRepository(AppDbContext dbContext) : IDatasetRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<Dataset>> GetDatasetsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Datasets
            .AsNoTracking()
            .OrderBy(dataset => dataset.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DatasetField>> GetDatasetFieldsAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DatasetFields
            .AsNoTracking()
            .Where(field => field.DatasetId == datasetId)
            .OrderBy(field => field.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DatasetExistsAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Datasets
            .AsNoTracking()
            .AnyAsync(dataset => dataset.Id == datasetId, cancellationToken);
    }
}
