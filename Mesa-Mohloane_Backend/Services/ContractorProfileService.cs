using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class ContractorProfileService : IContractorProfileService
{
    private readonly IContractorProfileRepository _profileRepo;
    private readonly IAuditRepository _audit;
    private readonly INotificationService _notifications;

    public ContractorProfileService(
        IContractorProfileRepository profileRepo,
        IAuditRepository audit,
        INotificationService notifications)
    {
        _profileRepo = profileRepo;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<ServiceResult<ContractorProfileDto>> CreateAsync(
        ContractorProfileCreateDto dto)
    {
        var existing = await _profileRepo.GetByUserIdAsync(dto.UserId);
        if (existing is not null)
            return ServiceResult<ContractorProfileDto>.Fail(
                "A contractor profile already exists for this user.");

        var profile = new ContractorProfile
        {
            UserId = dto.UserId,
            CompanyName = dto.CompanyName.Trim(),
            RegistrationNumber = dto.RegistrationNumber.Trim(),
            TaxNumber = dto.TaxNumber?.Trim(),
            CoverageArea = dto.CoverageArea.Trim(),
            IsApproved = false,
            AverageRating = 0,
            CompletedJobsCount = 0,
            LateCompletionCount = 0
        };

        var id = await _profileRepo.CreateAsync(profile);

        await _audit.LogAsync("ContractorProfileCreated", "ContractorProfile",
            id.ToString(), dto.UserId.ToString(),
            $"Company: {dto.CompanyName}");

        return await GetByIdAsync(id);
    }

    public async Task<ServiceResult<ContractorProfileDto>> UpdateAsync(
        Guid id, ContractorProfileUpdateDto dto, Guid requesterId)
    {
        var profile = await _profileRepo.GetByIdAsync(id);
        if (profile is null)
            return ServiceResult<ContractorProfileDto>.Fail("Contractor profile not found.");

        // Only the profile owner or admin can update
        if (profile.UserId != requesterId)
            return ServiceResult<ContractorProfileDto>.Fail(
                "You are not authorised to update this profile.");

        profile.CompanyName = dto.CompanyName.Trim();
        profile.RegistrationNumber = dto.RegistrationNumber.Trim();
        profile.TaxNumber = dto.TaxNumber?.Trim();
        profile.CoverageArea = dto.CoverageArea.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        await _profileRepo.UpdateAsync(profile);

        await _audit.LogAsync("ContractorProfileUpdated", "ContractorProfile",
            id.ToString(), requesterId.ToString(), "Profile details updated");

        return await GetByIdAsync(id);
    }

    public async Task<ServiceResult<ContractorProfileDto>> ApproveAsync(
        Guid id, Guid adminId)
    {
        var profile = await _profileRepo.GetByIdAsync(id);
        if (profile is null)
            return ServiceResult<ContractorProfileDto>.Fail("Contractor profile not found.");

        if (profile.IsApproved)
            return ServiceResult<ContractorProfileDto>.Fail("Profile is already approved.");

        profile.IsApproved = true;
        profile.ApprovedByAdminId = adminId;
        profile.ApprovedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        await _profileRepo.UpdateAsync(profile);

        // Notify the contractor their profile is approved
        await _notifications.SendAsync(
            userId: profile.UserId,
            type: NotificationType.AssignmentCreated,
            title: "Contractor Profile Approved",
            message: "Your contractor profile has been approved. You can now submit tenders.",
            relatedEntityName: "ContractorProfile",
            relatedEntityId: id);

        await _audit.LogAsync("ContractorProfileApproved", "ContractorProfile",
            id.ToString(), adminId.ToString(),
            $"Company: {profile.CompanyName} approved");

        return await GetByIdAsync(id);
    }

    public async Task<ServiceResult<ContractorProfileDto>> GetByIdAsync(Guid id)
    {
        var profile = await _profileRepo.GetByIdAsync(id);
        if (profile is null)
            return ServiceResult<ContractorProfileDto>.Fail("Contractor profile not found.");
        return ServiceResult<ContractorProfileDto>.Ok(MapToDto(profile));
    }

    public async Task<ServiceResult<ContractorProfileDto>> GetByUserIdAsync(Guid userId)
    {
        var profile = await _profileRepo.GetByUserIdAsync(userId);
        if (profile is null)
            return ServiceResult<ContractorProfileDto>.Fail("No contractor profile found for this user.");
        return ServiceResult<ContractorProfileDto>.Ok(MapToDto(profile));
    }

    public async Task<PagedResultDto<ContractorProfileDto>> GetAllApprovedAsync(
        int page, int pageSize)
    {
        var items = await _profileRepo.GetAllApprovedAsync(page, pageSize);
        var total = await _profileRepo.GetTotalApprovedCountAsync();
        return new PagedResultDto<ContractorProfileDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultDto<ContractorProfileDto>> GetAllAsync(
    int page,
    int pageSize,
    bool? isApproved = null)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var items = await _profileRepo.GetAllAsync(page, pageSize, isApproved);
        var total = await _profileRepo.GetTotalCountAsync(isApproved);

        return new PagedResultDto<ContractorProfileDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static ContractorProfileDto MapToDto(ContractorProfile p) => new(
        p.Id, p.UserId, p.CompanyName, p.RegistrationNumber, p.TaxNumber,
        p.CoverageArea, p.AverageRating, p.CompletedJobsCount,
        p.LateCompletionCount, p.IsApproved, p.ApprovedByAdminId, p.ApprovedAt);
}