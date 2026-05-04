using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class ContractorRatingService : IContractorRatingService
{
    private readonly IContractorRatingRepository _ratingRepo;
    private readonly IContractorProfileRepository _profileRepo;
    private readonly IAssignmentRepository _assignmentRepo;
    private readonly IIncidentRepository _incidentRepo;
    private readonly IAuditRepository _audit;
    private readonly INotificationService _notifications;

    public ContractorRatingService(
        IContractorRatingRepository ratingRepo,
        IContractorProfileRepository profileRepo,
        IAssignmentRepository assignmentRepo,
        IIncidentRepository incidentRepo,
        IAuditRepository audit,
        INotificationService notifications)
    {
        _ratingRepo = ratingRepo;
        _profileRepo = profileRepo;
        _assignmentRepo = assignmentRepo;
        _incidentRepo = incidentRepo;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<ServiceResult<ContractorRatingDto>> RateAsync(
        Guid citizenId, ContractorRatingCreateDto dto)
    {
        // Verify citizen owns the incident
        var incident = await _incidentRepo.GetByIdAsync(dto.IncidentId);
        if (incident is null || incident.CitizenId != citizenId)
            return ServiceResult<ContractorRatingDto>.Fail(
                "You are not authorised to rate this job.");

        // Assignment must be Closed (payment disbursed) before rating
        var assignment = await _assignmentRepo.GetByIdAsync(dto.AssignmentId);
        if (assignment is null)
            return ServiceResult<ContractorRatingDto>.Fail("Assignment not found.");

        if (assignment.Status != AssignmentStatus.Closed)
            return ServiceResult<ContractorRatingDto>.Fail(
                "You can only rate a contractor after the job has been fully closed and payment disbursed.");

        // One rating per assignment per citizen
        if (await _ratingRepo.HasCitizenRatedAsync(dto.AssignmentId, citizenId))
            return ServiceResult<ContractorRatingDto>.Fail(
                "You have already rated this contractor for this job.");

        // Stars must be 1–5
        if (dto.Stars < 1 || dto.Stars > 5)
            return ServiceResult<ContractorRatingDto>.Fail(
                "Rating must be between 1 and 5 stars.");

        var rating = new ContractorRating
        {
            IncidentId = dto.IncidentId,
            AssignmentId = dto.AssignmentId,
            CitizenId = citizenId,
            ContractorId = dto.ContractorId,
            Stars = dto.Stars,
            Comment = dto.Comment?.Trim(),
            RatedAt = DateTime.UtcNow
        };

        var id = await _ratingRepo.CreateAsync(rating);

        // Recalculate and persist the contractor's new average rating
        var newAverage = await _ratingRepo.GetAverageRatingAsync(dto.ContractorId);
        var profile = await _profileRepo.GetByUserIdAsync(dto.ContractorId);
        if (profile is not null)
        {
            profile.AverageRating = Math.Round(newAverage, 2);
            profile.UpdatedAt = DateTime.UtcNow;
            await _profileRepo.UpdateAsync(profile);
        }

        // Notify the contractor they received a rating
        await _notifications.SendAsync(
            userId: dto.ContractorId,
            type: NotificationType.ContractorRated,
            title: $"You received a {dto.Stars}-star rating",
            message: dto.Comment is not null
                ? $"A citizen rated your work {dto.Stars}/5: \"{dto.Comment}\""
                : $"A citizen rated your work {dto.Stars}/5.",
            relatedEntityName: "ContractorRating",
            relatedEntityId: id);

        await _audit.LogAsync("ContractorRated", "ContractorRating",
            id.ToString(), citizenId.ToString(),
            $"Contractor {dto.ContractorId} rated {dto.Stars}/5 stars");

        return await GetByIdAsync(id);
    }

    public async Task<ServiceResult<ContractorRatingDto>> GetByIdAsync(Guid id)
    {
        var r = await _ratingRepo.GetByIdAsync(id);
        if (r is null) return ServiceResult<ContractorRatingDto>.Fail("Rating not found.");
        return ServiceResult<ContractorRatingDto>.Ok(MapToDto(r));
    }

    public async Task<ServiceResult<ContractorRatingDto>> GetByAssignmentAsync(Guid assignmentId)
    {
        var r = await _ratingRepo.GetByAssignmentAsync(assignmentId);
        if (r is null) return ServiceResult<ContractorRatingDto>.Fail("No rating found for this assignment.");
        return ServiceResult<ContractorRatingDto>.Ok(MapToDto(r));
    }

    public async Task<PagedResultDto<ContractorRatingDto>> GetByContractorAsync(
        Guid contractorId, int page, int pageSize)
    {
        var items = await _ratingRepo.GetByContractorAsync(contractorId, page, pageSize);
        var total = await _ratingRepo.GetCountByContractorAsync(contractorId);
        return new PagedResultDto<ContractorRatingDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static ContractorRatingDto MapToDto(ContractorRating r) => new(
        r.Id, r.IncidentId, r.AssignmentId, r.CitizenId,
        r.ContractorId, r.Stars, r.Comment, r.RatedAt);
}