using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface IIncidentService
{
    // Citizen operations
    Task<ServiceResult<IncidentDto>> CreateAsync(Guid citizenId, IncidentCreateDto dto, IList<IFormFile> photos);
    Task<ServiceResult<IncidentDto>> UpdateAsync(Guid incidentId, Guid citizenId, IncidentUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid incidentId, Guid citizenId);
    Task<ServiceResult<IncidentDto>> AddPhotosAsync(Guid incidentId, Guid citizenId, IList<IFormFile> photos);
    Task<ServiceResult> DeletePhotoAsync(Guid incidentId, Guid photoId, Guid citizenId);

    // Admin operations
    Task<ServiceResult<IncidentDto>> VerifyAsync(Guid incidentId, Guid adminId);
    Task<ServiceResult<IncidentDto>> RejectAsync(Guid incidentId, Guid adminId, string reason);
    Task<ServiceResult<IncidentDto>> PublishForBiddingAsync(Guid incidentId, Guid adminId);


    // Queries
    Task<ServiceResult<IncidentDto>> GetByIdAsync(Guid id);
    Task<PagedResultDto<IncidentListDto>> GetByCitizenAsync(Guid citizenId, int page, int pageSize);
    Task<PagedResultDto<IncidentListDto>> GetAllAsync(int page, int pageSize, IncidentStatus? status, string? search);
    Task<PagedResultDto<IncidentListDto>> GetPublishedAsync(int page, int pageSize, string? search);
}