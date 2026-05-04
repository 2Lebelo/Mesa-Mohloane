namespace Mesa_Mohloane_Frontend.Dtos;

public sealed record InvoiceLineItemDto(
    Guid Id,
    int Category,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record InvoiceLineItemCreateDto(
    int Category,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record InvoiceDto(
    Guid Id,
    Guid AssignmentId,
    Guid TenderApplicationId,
    Guid ContractorId,
    string InvoiceNumber,
    decimal OriginalQuotedAmount,
    decimal FinalInvoiceAmount,
    decimal VariancePercentage,
    bool IsVarianceFlagged,
    int Status,
    DateTime SubmittedAt,
    DateTime? ValidatedAt,
    DateTime? ApprovedAt,
    Guid? ApprovedByAdminId,
    DateTime? DisbursedAt,
    DateTime? CitizenAcknowledgedAt,
    string? ValidationRemarks,
    IReadOnlyCollection<InvoiceLineItemDto> LineItems);

public sealed record InvoiceListDto(
    Guid Id,
    string InvoiceNumber,
    decimal FinalInvoiceAmount,
    int Status,
    DateTime SubmittedAt);

public sealed record InvoiceCreateDto(
    Guid AssignmentId,
    Guid TenderApplicationId,
    decimal OriginalQuotedAmount,
    decimal FinalInvoiceAmount,
    IReadOnlyCollection<InvoiceLineItemCreateDto> LineItems);
