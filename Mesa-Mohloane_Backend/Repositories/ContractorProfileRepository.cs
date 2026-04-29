using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mesa_Mohloane_Backend.Repositories;

public class ContractorProfileRepository : IContractorProfileRepository
{
    private readonly MesaMohloaneDbContext _context;

    public ContractorProfileRepository(MesaMohloaneDbContext context)
        => _context = context;

    public async Task<ContractorProfile?> GetByIdAsync(Guid id)
        => await _context.ContractorProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

    public async Task<ContractorProfile?> GetByUserIdAsync(Guid userId)
        => await _context.ContractorProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

    public async Task<IEnumerable<ContractorProfile>> GetAllApprovedAsync(int page, int pageSize)
        => await _context.ContractorProfiles
            .Include(p => p.User)
            .Where(p => p.IsApproved && !p.IsDeleted)
            .OrderBy(p => p.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetTotalApprovedCountAsync()
        => await _context.ContractorProfiles
            .CountAsync(p => p.IsApproved && !p.IsDeleted);

    public async Task<Guid> CreateAsync(ContractorProfile profile)
    {
        _context.ContractorProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return profile.Id;
    }

    public async Task UpdateAsync(ContractorProfile profile)
    {
        _context.ContractorProfiles.Update(profile);
        await _context.SaveChangesAsync();
    }
}