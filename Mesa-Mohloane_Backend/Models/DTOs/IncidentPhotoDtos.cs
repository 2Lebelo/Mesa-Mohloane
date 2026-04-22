namespace Mesa_Mohloane_Backend.Models.DTOs;

public record IncidentPhotoDto(
    Guid Id,
    Guid IncidentId,
    string ImageUrl,
    string? PublicId,
    string? Caption);

public record IncidentPhotoCreateDto(
    string ImageUrl,
    string? PublicId,
    string? Caption);
