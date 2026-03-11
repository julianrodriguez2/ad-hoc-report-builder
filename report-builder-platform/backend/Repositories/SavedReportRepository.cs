using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class SavedReportRepository(AppDbContext dbContext) : ISavedReportRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<SavedReport>> GetSavedReportsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SavedReports.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<SavedReport?> GetSavedReportByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SavedReports.AsNoTracking().FirstOrDefaultAsync(report => report.Id == id, cancellationToken);
    }

    public async Task<SavedReport> AddSavedReportAsync(SavedReport report, CancellationToken cancellationToken = default)
    {
        _dbContext.SavedReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return report;
    }
}
