using backend.Models;

namespace backend.Repositories;

public interface IDatasetRepository
{
    Task<IReadOnlyList<Dataset>> GetDatasetsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatasetField>> GetDatasetFieldsAsync(Guid datasetId, CancellationToken cancellationToken = default);

    Task<bool> DatasetExistsAsync(Guid datasetId, CancellationToken cancellationToken = default);
}
