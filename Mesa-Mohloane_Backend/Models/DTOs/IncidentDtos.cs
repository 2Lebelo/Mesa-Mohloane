using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Models.DTOs;

public record IncidentDto(
    Guid Id,
    string IncidentNumber,
    string Title,
    string Description,
    string LocationName,
    decimal Latitude,
    decimal Longitude,
    IncidentStatus Status,
    DateTime ReportedAt,
    DateTime? VerifiedAt,
    DateTime? PublishedAt,
    DateTime? ClosedAt,
    Guid CitizenId,
    Guid? VerifiedByAdminId,
    IReadOnlyCollection<IncidentPhotoDto> Photos);

public record IncidentCreateDto(
    string Title,
    string Description,
    string LocationName,
    decimal Latitude,
    decimal Longitude,
    Guid CitizenId,
    IReadOnlyCollection<IncidentPhotoCreateDto> Photos);

public record IncidentUpdateDto(
    string Title,
    string Description,
    string LocationName,
    decimal Latitude,
    decimal Longitude,
    IncidentStatus Status);

public record IncidentListDto(
    Guid Id,
    string IncidentNumber,
    string Title,
    string LocationName,
    IncidentStatus Status,
    DateTime ReportedAt);
