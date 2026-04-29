namespace Mesa_Mohloane_Backend.Models.DTOs;

public record WorkCompletionDto(
    Guid Id,
    Guid AssignmentId,
    string CompletionSummary,
    string? CompletionEvidenceUrl,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedByAdminId);

public record WorkCompletionCreateDto(
    string CompletionSummary,
    string? CompletionEvidenceUrl);