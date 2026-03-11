using backend.Models;

namespace backend.Repositories;

public interface ISavedReportRepository
{
    Task<IReadOnlyList<SavedReport>> GetSavedReportsAsync(CancellationToken cancellationToken = default);

    Task<SavedReport?> GetSavedReportByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<SavedReport> AddSavedReportAsync(SavedReport report, CancellationToken cancellationToken = default);
}
