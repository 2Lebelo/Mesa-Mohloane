using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Models.DTOs;

public record AssignmentDto(
    Guid Id,
    Guid IncidentId,
    Guid TenderApplicationId,
    Guid ContractorId,
    Guid AssignedByAdminId,
    DateTime AssignedAt,
    AssignmentStatus Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? CitizenAcknowledgedAt,
    DateTime? AdminApprovedAt);
