namespace Mesa_Mohloane_Frontend.Dtos;

public sealed record AssignmentDto(
    Guid Id,
    Guid IncidentId,
    Guid TenderApplicationId,
    Guid ContractorId,
    Guid AssignedByAdminId,
    DateTime AssignedAt,
    int Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? CitizenAcknowledgedAt,
    DateTime? AdminApprovedAt);

public sealed record WorkCompletionDto(
    Guid Id,
    Guid AssignmentId,
    string CompletionSummary,
    string? CompletionEvidenceUrl,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedByAdminId);

public sealed record WorkCompletionCreateDto(
    string CompletionSummary,
    string? CompletionEvidenceUrl);
