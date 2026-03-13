using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class SavedReportRepository(AppDbContext dbContext) : ISavedReportRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<SavedReport>> GetSavedReportsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SavedReports
            .AsNoTracking()
            .OrderByDescending(report => report.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SavedReport?> GetSavedReportByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SavedReports
            .AsNoTracking()
            .FirstOrDefaultAsync(report => report.Id == id, cancellationToken);
    }

    public async Task<SavedReport?> GetSavedReportByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SavedReports
            .FirstOrDefaultAsync(report => report.Id == id, cancellationToken);
    }

    public async Task<SavedReport> AddSavedReportAsync(SavedReport report, CancellationToken cancellationToken = default)
    {
        _dbContext.SavedReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteSavedReportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _dbContext.SavedReports.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (report is null)
        {
            return false;
        }

        _dbContext.SavedReports.Remove(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
