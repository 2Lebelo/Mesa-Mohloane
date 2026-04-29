using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mesa_Mohloane_Backend.Repositories;

public class TenderApplicationRepository : ITenderApplicationRepository
{
    private readonly MesaMohloaneDbContext _context;

    public TenderApplicationRepository(MesaMohloaneDbContext context)
        => _context = context;

    public async Task<TenderApplication?> GetByIdAsync(Guid id)
        => await _context.TenderApplications
            .Include(t => t.LineItems)
            .Include(t => t.Contractor)
            .Include(t => t.Incident)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

    public async Task<IEnumerable<TenderApplication>> GetByIncidentAsync(Guid incidentId)
        => await _context.TenderApplications
            .Include(t => t.LineItems)
            .Include(t => t.Contractor)
            .Where(t => t.IncidentId == incidentId && !t.IsDeleted)
            .OrderByDescending(t => t.WeightedScore)
            .ToListAsync();

    public async Task<IEnumerable<TenderApplication>> GetByContractorAsync(
        Guid contractorId, int page, int pageSize)
        => await _context.TenderApplications
            .Include(t => t.Incident)
            .Where(t => t.ContractorId == contractorId && !t.IsDeleted)
            .OrderByDescending(t => t.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetCountByContractorAsync(Guid contractorId)
        => await _context.TenderApplications
            .CountAsync(t => t.ContractorId == contractorId && !t.IsDeleted);

    public async Task<bool> HasContractorAppliedAsync(Guid incidentId, Guid contractorId)
        => await _context.TenderApplications
            .AnyAsync(t => t.IncidentId == incidentId
                        && t.ContractorId == contractorId
                        && !t.IsDeleted
                        && t.Status != TenderStatus.Withdrawn);

    public async Task<decimal> GetMinBidForIncidentAsync(Guid incidentId)
        => await _context.TenderApplications
            .Where(t => t.IncidentId == incidentId
                     && !t.IsDeleted
                     && t.Status == TenderStatus.Submitted)
            .MinAsync(t => (decimal?)t.QuotedTotalAmount) ?? 0m;

    public async Task<Guid> CreateAsync(TenderApplication application)
    {
        _context.TenderApplications.Add(application);
        await _context.SaveChangesAsync();
        return application.Id;
    }

    public async Task UpdateAsync(TenderApplication application)
    {
        _context.TenderApplications.Update(application);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<TenderApplication> applications)
    {
        _context.TenderApplications.UpdateRange(applications);
        await _context.SaveChangesAsync();
    }
}