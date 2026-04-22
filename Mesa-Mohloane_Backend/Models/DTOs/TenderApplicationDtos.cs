using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Models.DTOs;

public record TenderApplicationDto(
    Guid Id,
    Guid IncidentId,
    Guid ContractorId,
    string ProposalText,
    int EstimatedTimelineDays,
    decimal QuotedTotalAmount,
    TenderStatus Status,
    DateTime SubmittedAt,
    decimal WeightedScore,
    decimal CostScore,
    decimal RatingScore,
    decimal PerformanceScore,
    int RankPosition,
    string? EvaluationNotes,
    IReadOnlyCollection<TenderLineItemDto> LineItems);

public record TenderApplicationCreateDto(
    Guid IncidentId,
    Guid ContractorId,
    string ProposalText,
    int EstimatedTimelineDays,
    decimal QuotedTotalAmount,
    IReadOnlyCollection<TenderLineItemCreateDto> LineItems);

public record TenderApplicationUpdateDto(
    string ProposalText,
    int EstimatedTimelineDays,
    TenderStatus Status,
    decimal QuotedTotalAmount);

public record TenderApplicationListDto(
    Guid Id,
    Guid IncidentId,
    Guid ContractorId,
    TenderStatus Status,
    decimal QuotedTotalAmount,
    decimal WeightedScore,
    int RankPosition);
