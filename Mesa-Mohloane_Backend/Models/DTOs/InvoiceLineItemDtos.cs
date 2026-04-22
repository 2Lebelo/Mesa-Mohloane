using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Models.DTOs;

public record InvoiceLineItemDto(
    Guid Id,
    TenderLineItemCategory Category,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal);

public record InvoiceLineItemCreateDto(
    TenderLineItemCategory Category,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal);
