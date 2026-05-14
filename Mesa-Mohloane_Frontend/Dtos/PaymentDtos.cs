namespace Mesa_Mohloane_Frontend.Dtos;

public sealed record PaymentDto(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string PaymentReference,
    string Method,
    int Status,
    DateTime InitiatedAt,
    DateTime? ApprovedAt,
    Guid? ApprovedByAdminId,
    DateTime? DisbursedAt,
    string? FailureReason);

public sealed record PaymentCreateDto(
    Guid InvoiceId,
    decimal Amount,
    string PaymentReference,
    string Method);
