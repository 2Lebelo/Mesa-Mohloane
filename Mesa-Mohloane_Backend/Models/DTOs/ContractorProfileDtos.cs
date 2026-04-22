namespace Mesa_Mohloane_Backend.Models.DTOs;

public record ContractorProfileDto(
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

public record ContractorProfileCreateDto(
    Guid UserId,
    string CompanyName,
    string RegistrationNumber,
    string? TaxNumber,
    string CoverageArea);

public record ContractorProfileUpdateDto(
    string CompanyName,
    string RegistrationNumber,
    string? TaxNumber,
    string CoverageArea,
    bool IsApproved);
