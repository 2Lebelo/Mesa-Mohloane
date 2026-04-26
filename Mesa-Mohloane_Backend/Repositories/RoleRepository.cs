using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mesa_Mohloane_Backend.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly MesaMohloaneDbContext _context;

    public RoleRepository(MesaMohloaneDbContext context)
        => _context = context;

    public async Task<Role?> GetByIdAsync(Guid id)           // was int
        => await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Role?> GetByNameAsync(string name)
        => await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);

    public async Task<IEnumerable<Role>> GetAllAsync()
        => await _context.Roles.ToListAsync();
}