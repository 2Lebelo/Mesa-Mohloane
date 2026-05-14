namespace Mesa_Mohloane_Frontend.Dtos;

public sealed record IncidentListItemDto(
    Guid Id,
    string IncidentNumber,
    string Title,
    string LocationName,
    int Status,
    DateTime ReportedAt);

public sealed record IncidentDetailDto(
    Guid Id,
    string IncidentNumber,
    string Title,
    string Description,
    string LocationName,
    decimal Latitude,
    decimal Longitude,
    int Status,
    DateTime ReportedAt,
    DateTime? VerifiedAt,
    DateTime? PublishedAt,
    DateTime? ClosedAt,
    Guid CitizenId,
    Guid? VerifiedByAdminId,
    IReadOnlyList<IncidentPhotoDto> Photos);

public sealed record IncidentPhotoDto(
    Guid Id,
    Guid IncidentId,
    string ImageUrl,
    string? PublicId,
    string? Caption);

public sealed record IncidentUpdateRequestDto(
    string Title,
    string Description,
    string LocationName,
    decimal Latitude,
    decimal Longitude,
    int Status);
