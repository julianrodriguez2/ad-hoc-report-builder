using backend.Models;

namespace backend.Repositories;

public interface ISavedReportRepository
{
    Task<IReadOnlyList<SavedReport>> GetSavedReportsAsync(CancellationToken cancellationToken = default);

    Task<SavedReport?> GetSavedReportByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SavedReport?> GetSavedReportByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SavedReport> AddSavedReportAsync(SavedReport report, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> DeleteSavedReportAsync(Guid id, CancellationToken cancellationToken = default);
}
