using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Models.DTOs;

public record TenderLineItemDto(
    Guid Id,
    TenderLineItemCategory Category,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal);

public record TenderLineItemCreateDto(
    TenderLineItemCategory Category,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal);
