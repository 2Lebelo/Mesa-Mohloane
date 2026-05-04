namespace Mesa_Mohloane_Frontend.Dtos;

public sealed record ContractorRatingDto(
    Guid Id,
    Guid IncidentId,
    Guid AssignmentId,
    Guid CitizenId,
    Guid ContractorId,
    int Stars,
    string? Comment,
    DateTime RatedAt);

public sealed record ContractorRatingCreateDto(
    Guid IncidentId,
    Guid AssignmentId,
    Guid ContractorId,
    int Stars,
    string? Comment);
