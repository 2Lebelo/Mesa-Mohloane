namespace Mesa_Mohloane_Backend.Models.DTOs;

public record ContractorRatingDto(
    Guid Id,
    Guid IncidentId,
    Guid AssignmentId,
    Guid CitizenId,
    Guid ContractorId,
    int Stars,
    string? Comment,
    DateTime RatedAt);

public record ContractorRatingCreateDto(
    Guid IncidentId,
    Guid AssignmentId,
    Guid CitizenId,
    Guid ContractorId,
    int Stars,
    string? Comment);
