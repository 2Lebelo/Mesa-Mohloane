namespace Mesa_Mohloane_Frontend.Dtos;

public sealed record TenderLineItemDto(
    Guid Id,
    int Category,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record TenderLineItemCreateDto(
    int Category,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record TenderApplicationDto(
    Guid Id,
    Guid IncidentId,
    Guid ContractorId,
    string ProposalText,
    int EstimatedTimelineDays,
    decimal QuotedTotalAmount,
    int Status,
    DateTime SubmittedAt,
    decimal WeightedScore,
    decimal CostScore,
    decimal RatingScore,
    decimal PerformanceScore,
    int RankPosition,
    string? EvaluationNotes,
    IReadOnlyCollection<TenderLineItemDto> LineItems);

public sealed record TenderApplicationListDto(
    Guid Id,
    Guid IncidentId,
    Guid ContractorId,
    int Status,
    decimal QuotedTotalAmount,
    decimal WeightedScore,
    int RankPosition);

public sealed record TenderApplicationCreateDto(
    Guid IncidentId,
    Guid ContractorId,
    string ProposalText,
    int EstimatedTimelineDays,
    decimal QuotedTotalAmount,
    IReadOnlyCollection<TenderLineItemCreateDto> LineItems);

public sealed record TenderApplicationUpdateDto(
    string ProposalText,
    int EstimatedTimelineDays,
    int Status,
    decimal QuotedTotalAmount);
