using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync(int page, int pageSize, string? search, Guid? roleId, bool? isActive);
    Task<int> GetTotalCountAsync(string? search, Guid? roleId, bool? isActive);
    Task<Guid> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneNumberExistsAsync(string phoneNumber);
    Task<IEnumerable<User>> GetContractorsAsync(int page, int pageSize, string? search);
    Task<IEnumerable<User>> GetCitizensAsync(int page, int pageSize, string? search);
    Task<IEnumerable<User>> GetAdministratorsAsync(int page, int pageSize, string? search);
    Task<IEnumerable<User>> GetAuditorsAsync(int page, int pageSize, string? search);
    Task<int> GetUserIncidentsCountAsync(Guid userId);
    Task<int> GetUserTenderApplicationsCountAsync(Guid userId);
    Task<bool> UpdatePasswordAsync(Guid userId, string passwordHash);
    Task<bool> ToggleActiveStatusAsync(Guid userId, bool isActive);
}