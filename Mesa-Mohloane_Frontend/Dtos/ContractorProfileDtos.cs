namespace Mesa_Mohloane_Frontend.Dtos;

public sealed record ContractorProfileDto(
    Guid Id,
    Guid UserId,
    string CompanyName,
    string RegistrationNumber,
    string? TaxNumber,
    string CoverageArea,
    decimal AverageRating,
    int CompletedJobsCount,
    int LateCompletionCount,
    bool IsApproved,
    Guid? ApprovedByAdminId,
    DateTime? ApprovedAt);

public sealed record ContractorProfileCreateDto(
    Guid UserId,
    string CompanyName,
    string RegistrationNumber,
    string? TaxNumber,
    string CoverageArea);

public sealed record ContractorProfileUpdateDto(
    string CompanyName,
    string RegistrationNumber,
    string? TaxNumber,
    string CoverageArea);
