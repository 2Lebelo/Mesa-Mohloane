using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Models.DTOs;

public record InvoiceDto(
    Guid Id,
    Guid AssignmentId,
    Guid TenderApplicationId,
    Guid ContractorId,
    string InvoiceNumber,
    decimal OriginalQuotedAmount,
    decimal FinalInvoiceAmount,
    decimal VariancePercentage,
    bool IsVarianceFlagged,
    InvoiceStatus Status,
    DateTime SubmittedAt,
    DateTime? ValidatedAt,
    DateTime? ApprovedAt,
    Guid? ApprovedByAdminId,
    DateTime? DisbursedAt,
    DateTime? CitizenAcknowledgedAt,
    string? ValidationRemarks,
    IReadOnlyCollection<InvoiceLineItemDto> LineItems);

public record InvoiceCreateDto(
    Guid AssignmentId,
    Guid TenderApplicationId,
    Guid ContractorId,
    string InvoiceNumber,
    decimal OriginalQuotedAmount,
    decimal FinalInvoiceAmount,
    IReadOnlyCollection<InvoiceLineItemCreateDto> LineItems);

public record InvoiceUpdateDto(
    decimal FinalInvoiceAmount,
    InvoiceStatus Status,
    string? ValidationRemarks);

public record InvoiceListDto(
    Guid Id,
    string InvoiceNumber,
    decimal FinalInvoiceAmount,
    InvoiceStatus Status,
    DateTime SubmittedAt);
