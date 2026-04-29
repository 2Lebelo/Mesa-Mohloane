using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mesa_Mohloane_Backend.Repositories;

public class ContractorRatingRepository : IContractorRatingRepository
{
    private readonly MesaMohloaneDbContext _context;

    public ContractorRatingRepository(MesaMohloaneDbContext context)
        => _context = context;

    public async Task<ContractorRating?> GetByIdAsync(Guid id)
        => await _context.ContractorRatings
            .Include(r => r.Citizen)
            .Include(r => r.Contractor)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

    public async Task<ContractorRating?> GetByAssignmentAsync(Guid assignmentId)
        => await _context.ContractorRatings
            .FirstOrDefaultAsync(r => r.AssignmentId == assignmentId && !r.IsDeleted);

    public async Task<IEnumerable<ContractorRating>> GetByContractorAsync(
        Guid contractorId, int page, int pageSize)
        => await _context.ContractorRatings
            .Include(r => r.Citizen)
            .Where(r => r.ContractorId == contractorId && !r.IsDeleted)
            .OrderByDescending(r => r.RatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetCountByContractorAsync(Guid contractorId)
        => await _context.ContractorRatings
            .CountAsync(r => r.ContractorId == contractorId && !r.IsDeleted);

    public async Task<decimal> GetAverageRatingAsync(Guid contractorId)
    {
        var avg = await _context.ContractorRatings
            .Where(r => r.ContractorId == contractorId && !r.IsDeleted)
            .AverageAsync(r => (double?)r.Stars);
        return (decimal)(avg ?? 0.0);
    }

    public async Task<bool> HasCitizenRatedAsync(Guid assignmentId, Guid citizenId)
        => await _context.ContractorRatings
            .AnyAsync(r => r.AssignmentId == assignmentId
                        && r.CitizenId == citizenId
                        && !r.IsDeleted);

    public async Task<Guid> CreateAsync(ContractorRating rating)
    {
        _context.ContractorRatings.Add(rating);
        await _context.SaveChangesAsync();
        return rating.Id;
    }
}