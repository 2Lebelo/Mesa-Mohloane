using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepo;
    private readonly ITenderApplicationRepository _tenderRepo;
    private readonly IIncidentRepository _incidentRepo;
    private readonly IContractorProfileRepository _profileRepo;
    private readonly IAuditRepository _audit;

    public AssignmentService(
        IAssignmentRepository assignmentRepo,
        ITenderApplicationRepository tenderRepo,
        IIncidentRepository incidentRepo,
        IContractorProfileRepository profileRepo,
        IAuditRepository audit)
    {
        _assignmentRepo = assignmentRepo;
        _tenderRepo = tenderRepo;
        _incidentRepo = incidentRepo;
        _profileRepo = profileRepo;
        _audit = audit;
    }

    // ── Admin: assign the winning contractor ──────────────────────────────────
    public async Task<ServiceResult<AssignmentDto>> AssignContractorAsync(
        Guid incidentId, Guid tenderApplicationId, Guid adminId)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (incident is null)
            return ServiceResult<AssignmentDto>.Fail("Incident not found.");

        if (incident.Status != IncidentStatus.Published)
            return ServiceResult<AssignmentDto>.Fail(
                "Incident must be in Published status before a contractor can be assigned.");

        var winningTender = await _tenderRepo.GetByIdAsync(tenderApplicationId);
        if (winningTender is null || winningTender.IncidentId != incidentId)
            return ServiceResult<AssignmentDto>.Fail(
                "Tender application not found for this incident.");

        if (winningTender.Status != TenderStatus.UnderReview &&
            winningTender.Status != TenderStatus.Submitted)
            return ServiceResult<AssignmentDto>.Fail(
                "Selected tender must be in Submitted or UnderReview status.");

        // Create the assignment
        var assignment = new Assignment
        {
            IncidentId = incidentId,
            TenderApplicationId = tenderApplicationId,
            ContractorId = winningTender.ContractorId,
            AssignedByAdminId = adminId,
            AssignedAt = DateTime.UtcNow,
            Status = AssignmentStatus.Assigned
        };

        var assignmentId = await _assignmentRepo.CreateAsync(assignment);

        // Update winning tender to Approved
        winningTender.Status = TenderStatus.Approved;
        winningTender.UpdatedAt = DateTime.UtcNow;

        // Reject all other tenders for this incident
        var allTenders = await _tenderRepo.GetByIncidentAsync(incidentId);
        var losers = allTenders
            .Where(t => t.Id != tenderApplicationId &&
                        t.Status != TenderStatus.Withdrawn)
            .ToList();

        foreach (var t in losers)
        {
            t.Status = TenderStatus.Rejected;
            t.UpdatedAt = DateTime.UtcNow;
        }

        losers.Add(winningTender);
        await _tenderRepo.UpdateRangeAsync(losers);

        // Advance incident status
        incident.Status = IncidentStatus.Assigned;
        incident.UpdatedAt = DateTime.UtcNow;
        await _incidentRepo.UpdateAsync(incident);

        await _audit.LogAsync("ContractorAssigned", "Assignment",
            assignmentId.ToString(), adminId.ToString(),
            $"Contractor {winningTender.ContractorId} assigned to Incident {incident.IncidentNumber}");

        return await GetByIdAsync(assignmentId);
    }

    // ── Contractor: start work ────────────────────────────────────────────────
    public async Task<ServiceResult<AssignmentDto>> StartWorkAsync(
        Guid assignmentId, Guid contractorId)
    {
        var assignment = await _assignmentRepo.GetByIdAsync(assignmentId);
        if (assignment is null)
            return ServiceResult<AssignmentDto>.Fail("Assignment not found.");

        if (assignment.ContractorId != contractorId)
            return ServiceResult<AssignmentDto>.Fail(
                "You are not authorised to start this assignment.");

        if (assignment.Status != AssignmentStatus.Assigned)
            return ServiceResult<AssignmentDto>.Fail(
                "Assignment must be in Assigned status to start work.");

        assignment.Status = AssignmentStatus.Started;
        assignment.StartedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _assignmentRepo.UpdateAsync(assignment);

        // Advance incident to InProgress
        var incident = await _incidentRepo.GetByIdAsync(assignment.IncidentId);
        if (incident is not null)
        {
            incident.Status = IncidentStatus.InProgress;
            incident.UpdatedAt = DateTime.UtcNow;
            await _incidentRepo.UpdateAsync(incident);
        }

        await _audit.LogAsync("WorkStarted", "Assignment",
            assignmentId.ToString(), contractorId.ToString(),
            "Contractor started work on assignment");

        return await GetByIdAsync(assignmentId);
    }

    // ── Contractor: submit work completion ────────────────────────────────────
    public async Task<ServiceResult<WorkCompletionDto>> SubmitWorkCompletionAsync(
        Guid assignmentId, Guid contractorId, WorkCompletionCreateDto dto)
    {
        var assignment = await _assignmentRepo.GetByIdAsync(assignmentId);
        if (assignment is null)
            return ServiceResult<WorkCompletionDto>.Fail("Assignment not found.");

        if (assignment.ContractorId != contractorId)
            return ServiceResult<WorkCompletionDto>.Fail(
                "You are not authorised to submit completion for this assignment.");

        if (assignment.Status != AssignmentStatus.Started)
            return ServiceResult<WorkCompletionDto>.Fail(
                "Work must be started before completion can be submitted.");

        var existing = await _assignmentRepo.GetWorkCompletionAsync(assignmentId);
        if (existing is not null)
            return ServiceResult<WorkCompletionDto>.Fail(
                "A work completion report has already been submitted for this assignment.");

        var workCompletion = new WorkCompletion
        {
            AssignmentId = assignmentId,
            CompletionSummary = dto.CompletionSummary.Trim(),
            CompletionEvidenceUrl = dto.CompletionEvidenceUrl?.Trim(),
            SubmittedAt = DateTime.UtcNow
        };

        await _assignmentRepo.CreateWorkCompletionAsync(workCompletion);

        assignment.Status = AssignmentStatus.Completed;
        assignment.CompletedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _assignmentRepo.UpdateAsync(assignment);

        await _audit.LogAsync("WorkCompletionSubmitted", "Assignment",
            assignmentId.ToString(), contractorId.ToString(),
            "Contractor submitted work completion report");

        return ServiceResult<WorkCompletionDto>.Ok(new WorkCompletionDto(
            workCompletion.Id,
            workCompletion.AssignmentId,
            workCompletion.CompletionSummary,
            workCompletion.CompletionEvidenceUrl,
            workCompletion.SubmittedAt,
            workCompletion.ReviewedAt,
            workCompletion.ReviewedByAdminId));
    }

    // ── Citizen: acknowledge work completion ──────────────────────────────────
    public async Task<ServiceResult<AssignmentDto>> AcknowledgeCompletionAsync(
        Guid assignmentId, Guid citizenId)
    {
        var assignment = await _assignmentRepo.GetByIdAsync(assignmentId);
        if (assignment is null)
            return ServiceResult<AssignmentDto>.Fail("Assignment not found.");

        // Verify the citizen owns the incident
        var incident = await _incidentRepo.GetByIdAsync(assignment.IncidentId);
        if (incident is null || incident.CitizenId != citizenId)
            return ServiceResult<AssignmentDto>.Fail(
                "You are not authorised to acknowledge this assignment.");

        if (assignment.Status != AssignmentStatus.Completed)
            return ServiceResult<AssignmentDto>.Fail(
                "Work must be completed by the contractor before you can acknowledge it.");

        assignment.CitizenAcknowledgedAt = DateTime.UtcNow;
        assignment.Status = AssignmentStatus.AwaitingApproval;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _assignmentRepo.UpdateAsync(assignment);

        await _audit.LogAsync("WorkAcknowledgedByCitizen", "Assignment",
            assignmentId.ToString(), citizenId.ToString(),
            "Citizen acknowledged work completion");

        return await GetByIdAsync(assignmentId);
    }

    // ── Admin: approve completion ─────────────────────────────────────────────
    public async Task<ServiceResult<AssignmentDto>> ApproveCompletionAsync(
        Guid assignmentId, Guid adminId)
    {
        var assignment = await _assignmentRepo.GetByIdAsync(assignmentId);
        if (assignment is null)
            return ServiceResult<AssignmentDto>.Fail("Assignment not found.");

        if (assignment.Status != AssignmentStatus.AwaitingApproval)
            return ServiceResult<AssignmentDto>.Fail(
                "Assignment must be awaiting approval. " +
                "Citizen acknowledgement is required before admin approval.");

        assignment.Status = AssignmentStatus.Approved;
        assignment.AdminApprovedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _assignmentRepo.UpdateAsync(assignment);

        // Update contractor's completed jobs count
        var profile = await _profileRepo.GetByUserIdAsync(assignment.ContractorId);
        if (profile is not null)
        {
            profile.CompletedJobsCount++;
            profile.UpdatedAt = DateTime.UtcNow;
            await _profileRepo.UpdateAsync(profile);
        }

        // Advance incident to Completed
        var incident = await _incidentRepo.GetByIdAsync(assignment.IncidentId);
        if (incident is not null)
        {
            incident.Status = IncidentStatus.Completed;
            incident.UpdatedAt = DateTime.UtcNow;
            await _incidentRepo.UpdateAsync(incident);
        }

        await _audit.LogAsync("CompletionApproved", "Assignment",
            assignmentId.ToString(), adminId.ToString(),
            "Admin approved work completion");

        return await GetByIdAsync(assignmentId);
    }

    // ── Queries ───────────────────────────────────────────────────────────────
    public async Task<ServiceResult<AssignmentDto>> GetByIdAsync(Guid id)
    {
        var a = await _assignmentRepo.GetByIdAsync(id);
        if (a is null)
            return ServiceResult<AssignmentDto>.Fail("Assignment not found.");
        return ServiceResult<AssignmentDto>.Ok(MapToDto(a));
    }

    public async Task<ServiceResult<AssignmentDto>> GetByIncidentAsync(Guid incidentId)
    {
        var a = await _assignmentRepo.GetByIncidentAsync(incidentId);
        if (a is null)
            return ServiceResult<AssignmentDto>.Fail("No assignment found for this incident.");
        return ServiceResult<AssignmentDto>.Ok(MapToDto(a));
    }

    private static AssignmentDto MapToDto(Assignment a) => new(
        a.Id, a.IncidentId, a.TenderApplicationId, a.ContractorId,
        a.AssignedByAdminId, a.AssignedAt, a.Status, a.StartedAt,
        a.CompletedAt, a.CitizenAcknowledgedAt, a.AdminApprovedAt);
}