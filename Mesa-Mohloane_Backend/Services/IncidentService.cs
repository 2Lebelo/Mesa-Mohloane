using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class IncidentService : IIncidentService
{
    private static readonly HashSet<IncidentStatus> EditableStatuses =
        new() { IncidentStatus.Pending };

    private readonly IIncidentRepository _incidentRepo;
    private readonly IAuditRepository _audit;
    private readonly CloudinaryService _cloudinary;

    public IncidentService(
        IIncidentRepository incidentRepo,
        IAuditRepository audit,
        CloudinaryService cloudinary)
    {
        _incidentRepo = incidentRepo;
        _audit = audit;
        _cloudinary = cloudinary;
    }

    public async Task<ServiceResult<IncidentDto>> CreateAsync(
        Guid citizenId,
        IncidentCreateDto dto,
        IList<IFormFile> photos)
    {
        var incidentNumber = await _incidentRepo.GenerateIncidentNumberAsync();

        var incident = new Incident
        {
            CitizenId = citizenId,
            IncidentNumber = incidentNumber,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            LocationName = dto.LocationName.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Status = IncidentStatus.Pending,
            ReportedAt = DateTime.UtcNow
        };

        var incidentId = await _incidentRepo.CreateAsync(incident);

        foreach (var file in photos)
        {
            var (imageUrl, publicId) = await _cloudinary.UploadAsync(file, "incidents");

            await _incidentRepo.AddPhotoAsync(new IncidentPhoto
            {
                IncidentId = incidentId,
                ImageUrl = imageUrl,
                PublicId = publicId,
                Caption = file.FileName
            });
        }

        await _audit.LogAsync(
            action: "IncidentCreated",
            entityName: "Incident",
            entityId: incidentId,
            actorUserId: citizenId,
            notes: $"Incident: {incidentNumber}");

        return await GetByIdAsync(incidentId);
    }

    public async Task<ServiceResult<IncidentDto>> UpdateAsync(
        Guid incidentId,
        Guid citizenId,
        IncidentUpdateDto dto)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);

        if (incident is null)
            return ServiceResult<IncidentDto>.Fail("Incident not found.");

        if (incident.CitizenId != citizenId)
            return ServiceResult<IncidentDto>.Fail(
                "You are not authorised to edit this incident.");

        if (!EditableStatuses.Contains(incident.Status))
            return ServiceResult<IncidentDto>.Fail(
                "This incident can no longer be edited. It has already been reviewed by an administrator.");

        incident.Title = dto.Title.Trim();
        incident.Description = dto.Description.Trim();
        incident.LocationName = dto.LocationName.Trim();
        incident.Latitude = dto.Latitude;
        incident.Longitude = dto.Longitude;
        incident.UpdatedAt = DateTime.UtcNow;

        await _incidentRepo.UpdateAsync(incident);

        await _audit.LogAsync(
            action: "IncidentUpdated",
            entityName: "Incident",
            entityId: incidentId,
            actorUserId: citizenId,
            notes: $"Incident: {incident.IncidentNumber}");

        return await GetByIdAsync(incidentId);
    }

    public async Task<ServiceResult> DeleteAsync(Guid incidentId, Guid citizenId)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);

        if (incident is null)
            return ServiceResult.Fail("Incident not found.");

        if (incident.CitizenId != citizenId)
            return ServiceResult.Fail(
                "You are not authorised to delete this incident.");

        if (!EditableStatuses.Contains(incident.Status))
            return ServiceResult.Fail(
                "This incident can no longer be deleted. It has already been reviewed by an administrator.");

        foreach (var photo in incident.Photos)
        {
            if (!string.IsNullOrWhiteSpace(photo.PublicId))
                await _cloudinary.DeleteAsync(photo.PublicId);
        }

        await _incidentRepo.DeleteAsync(incidentId);

        await _audit.LogAsync(
            action: "IncidentDeleted",
            entityName: "Incident",
            entityId: incidentId,
            actorUserId: citizenId,
            notes: $"Incident: {incident.IncidentNumber}");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<IncidentDto>> AddPhotosAsync(
        Guid incidentId,
        Guid citizenId,
        IList<IFormFile> photos)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);

        if (incident is null)
            return ServiceResult<IncidentDto>.Fail("Incident not found.");

        if (incident.CitizenId != citizenId)
            return ServiceResult<IncidentDto>.Fail(
                "You are not authorised to add photos to this incident.");

        if (!EditableStatuses.Contains(incident.Status))
            return ServiceResult<IncidentDto>.Fail(
                "Photos cannot be added — the incident has already been reviewed.");

        foreach (var file in photos)
        {
            var (imageUrl, publicId) = await _cloudinary.UploadAsync(file, "incidents");

            await _incidentRepo.AddPhotoAsync(new IncidentPhoto
            {
                IncidentId = incidentId,
                ImageUrl = imageUrl,
                PublicId = publicId,
                Caption = file.FileName
            });
        }

        await _audit.LogAsync(
            action: "IncidentPhotosAdded",
            entityName: "Incident",
            entityId: incidentId,
            actorUserId: citizenId,
            notes: $"Added {photos.Count} photo(s) to incident: {incident.IncidentNumber}");

        return await GetByIdAsync(incidentId);
    }

    public async Task<ServiceResult> DeletePhotoAsync(
        Guid incidentId,
        Guid photoId,
        Guid citizenId)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);

        if (incident is null)
            return ServiceResult.Fail("Incident not found.");

        if (incident.CitizenId != citizenId)
            return ServiceResult.Fail(
                "You are not authorised to delete photos from this incident.");

        if (!EditableStatuses.Contains(incident.Status))
            return ServiceResult.Fail(
                "Photos cannot be removed — the incident has already been reviewed.");

        var photo = incident.Photos.FirstOrDefault(p => p.Id == photoId);

        if (photo is null)
            return ServiceResult.Fail("Photo not found.");

        if (!string.IsNullOrWhiteSpace(photo.PublicId))
            await _cloudinary.DeleteAsync(photo.PublicId);

        await _incidentRepo.DeletePhotoAsync(photoId);

        await _audit.LogAsync(
            action: "IncidentPhotoDeleted",
            entityName: "Incident",
            entityId: incidentId,
            actorUserId: citizenId,
            notes: $"Deleted photo from incident: {incident.IncidentNumber}");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<IncidentDto>> GetByIdAsync(Guid id)
    {
        var incident = await _incidentRepo.GetByIdAsync(id);

        if (incident is null)
            return ServiceResult<IncidentDto>.Fail("Incident not found.");

        return ServiceResult<IncidentDto>.Ok(MapToDto(incident));
    }

    public async Task<PagedResultDto<IncidentListDto>> GetByCitizenAsync(
        Guid citizenId,
        int page,
        int pageSize)
    {
        var items = await _incidentRepo.GetByCitizenAsync(citizenId, page, pageSize);
        var total = await _incidentRepo.GetCountByCitizenAsync(citizenId);

        return new PagedResultDto<IncidentListDto>
        {
            Items = items.Select(MapToListDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultDto<IncidentListDto>> GetAllAsync(
        int page,
        int pageSize,
        IncidentStatus? status,
        string? search)
    {
        var items = await _incidentRepo.GetAllAsync(page, pageSize, status, search);
        var total = await _incidentRepo.GetTotalCountAsync(status, search);

        return new PagedResultDto<IncidentListDto>
        {
            Items = items.Select(MapToListDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static IncidentDto MapToDto(Incident i) => new(
        Id: i.Id,
        IncidentNumber: i.IncidentNumber,
        Title: i.Title,
        Description: i.Description,
        LocationName: i.LocationName,
        Latitude: i.Latitude,
        Longitude: i.Longitude,
        Status: i.Status,
        ReportedAt: i.ReportedAt,
        VerifiedAt: i.VerifiedAt,
        PublishedAt: i.PublishedAt,
        ClosedAt: i.ClosedAt,
        CitizenId: i.CitizenId,
        VerifiedByAdminId: i.VerifiedByAdminId,
        Photos: i.Photos.Select(p => new IncidentPhotoDto(
            p.Id,
            p.IncidentId,
            p.ImageUrl,
            p.PublicId,
            p.Caption))
            .ToList()
            .AsReadOnly());

    private static IncidentListDto MapToListDto(Incident i) => new(
        Id: i.Id,
        IncidentNumber: i.IncidentNumber,
        Title: i.Title,
        LocationName: i.LocationName,
        Status: i.Status,
        ReportedAt: i.ReportedAt);
}