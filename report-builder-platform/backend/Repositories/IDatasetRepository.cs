using backend.Models;

namespace backend.Repositories;

public interface IDatasetRepository
{
    Task<IReadOnlyList<Dataset>> GetDatasetsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatasetField>> GetDatasetFieldsAsync(int datasetId, CancellationToken cancellationToken = default);
}
