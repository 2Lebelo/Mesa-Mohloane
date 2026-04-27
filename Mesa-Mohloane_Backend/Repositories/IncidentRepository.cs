using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mesa_Mohloane_Backend.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private readonly MesaMohloaneDbContext _context;

    public IncidentRepository(MesaMohloaneDbContext context)
        => _context = context;

    public async Task<Incident?> GetByIdAsync(Guid id)
        => await _context.Incidents
            .Include(i => i.Photos)
            .Include(i => i.Citizen)
            .Include(i => i.VerifiedByAdmin)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

    public async Task<Incident?> GetByIncidentNumberAsync(string incidentNumber)
        => await _context.Incidents
            .Include(i => i.Photos)
            .FirstOrDefaultAsync(i => i.IncidentNumber == incidentNumber && !i.IsDeleted);

    public async Task<IEnumerable<Incident>> GetByCitizenAsync(
        Guid citizenId, int page, int pageSize)
        => await _context.Incidents
            .Include(i => i.Photos)
            .Where(i => i.CitizenId == citizenId && !i.IsDeleted)
            .OrderByDescending(i => i.ReportedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<IEnumerable<Incident>> GetAllAsync(
        int page, int pageSize, IncidentStatus? status, string? search)
    {
        var query = _context.Incidents
            .Include(i => i.Photos)
            .Include(i => i.Citizen)
            .Where(i => !i.IsDeleted);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i =>
                i.Title.Contains(search) ||
                i.LocationName.Contains(search) ||
                i.IncidentNumber.Contains(search));

        return await query
            .OrderByDescending(i => i.ReportedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(IncidentStatus? status, string? search)
    {
        var query = _context.Incidents.Where(i => !i.IsDeleted);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i =>
                i.Title.Contains(search) ||
                i.LocationName.Contains(search) ||
                i.IncidentNumber.Contains(search));

        return await query.CountAsync();
    }

    public async Task<int> GetCountByCitizenAsync(Guid citizenId)
        => await _context.Incidents
            .CountAsync(i => i.CitizenId == citizenId && !i.IsDeleted);

    public async Task<Guid> CreateAsync(Incident incident)
    {
        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();
        return incident.Id;
    }

    public async Task UpdateAsync(Incident incident)
    {
        _context.Incidents.Update(incident);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var incident = await _context.Incidents.FindAsync(id);
        if (incident is null) return;

        // Soft delete — preserves audit history
        incident.IsDeleted = true;
        incident.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task AddPhotoAsync(IncidentPhoto photo)
    {
        _context.IncidentPhotos.Add(photo);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePhotoAsync(Guid photoId)
    {
        var photo = await _context.IncidentPhotos.FindAsync(photoId);
        if (photo is null) return;

        _context.IncidentPhotos.Remove(photo);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await _context.Incidents
            .AnyAsync(i => i.Id == id && !i.IsDeleted);

    /// <summary>
    /// Generates a unique incident number in the format INC-YYYYMMDD-XXXX.
    /// The sequence resets daily.
    /// </summary>
    public async Task<string> GenerateIncidentNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"INC-{today:yyyyMMdd}";

        var todayCount = await _context.Incidents
            .CountAsync(i => i.IncidentNumber.StartsWith(prefix));

        return $"{prefix}-{(todayCount + 1):D4}";
    }
}