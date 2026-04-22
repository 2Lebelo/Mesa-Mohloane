using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Models.DTOs;

public record PaymentDto(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string PaymentReference,
    string Method,
    PaymentStatus Status,
    DateTime InitiatedAt,
    DateTime? ApprovedAt,
    Guid? ApprovedByAdminId,
    DateTime? DisbursedAt,
    string? FailureReason);

public record PaymentCreateDto(
    Guid InvoiceId,
    decimal Amount,
    string PaymentReference,
    string Method);

public record PaymentUpdateDto(
    PaymentStatus Status,
    string? FailureReason);
