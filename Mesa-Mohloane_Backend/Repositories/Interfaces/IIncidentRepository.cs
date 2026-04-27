using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Repositories.Interfaces;

public interface IIncidentRepository
{
    // Queries
    Task<Incident?> GetByIdAsync(Guid id);
    Task<Incident?> GetByIncidentNumberAsync(string incidentNumber);
    Task<IEnumerable<Incident>> GetByCitizenAsync(Guid citizenId, int page, int pageSize);
    Task<IEnumerable<Incident>> GetAllAsync(int page, int pageSize, IncidentStatus? status, string? search);
    Task<int> GetTotalCountAsync(IncidentStatus? status, string? search);
    Task<int> GetCountByCitizenAsync(Guid citizenId);

    // Commands 
    Task<Guid> CreateAsync(Incident incident);
    Task UpdateAsync(Incident incident);
    Task DeleteAsync(Guid id);
    Task AddPhotoAsync(IncidentPhoto photo);
    Task DeletePhotoAsync(Guid photoId);

    // Helpers 
    Task<bool> ExistsAsync(Guid id);
    Task<string> GenerateIncidentNumberAsync();
}