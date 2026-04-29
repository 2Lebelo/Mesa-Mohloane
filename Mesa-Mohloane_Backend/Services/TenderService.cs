using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class TenderService : ITenderService
{
    private readonly ITenderApplicationRepository _tenderRepo;
    private readonly IContractorProfileRepository _profileRepo;
    private readonly IIncidentRepository _incidentRepo;
    private readonly IAuditRepository _audit;

    public TenderService(
        ITenderApplicationRepository tenderRepo,
        IContractorProfileRepository profileRepo,
        IIncidentRepository incidentRepo,
        IAuditRepository audit)
    {
        _tenderRepo = tenderRepo;
        _profileRepo = profileRepo;
        _incidentRepo = incidentRepo;
        _audit = audit;
    }

    // ── Submit tender (Contractor) ────────────────────────────────────────────
    public async Task<ServiceResult<TenderApplicationDto>> SubmitAsync(
        Guid contractorId, TenderApplicationCreateDto dto)
    {
        // Guard: incident must be Published
        var incident = await _incidentRepo.GetByIdAsync(dto.IncidentId);
        if (incident is null)
            return ServiceResult<TenderApplicationDto>.Fail("Incident not found.");

        if (incident.Status != IncidentStatus.Published)
            return ServiceResult<TenderApplicationDto>.Fail(
                "This incident is not open for bidding.");

        // Guard: contractor must have an approved profile
        var profile = await _profileRepo.GetByUserIdAsync(contractorId);
        if (profile is null || !profile.IsApproved)
            return ServiceResult<TenderApplicationDto>.Fail(
                "Your contractor profile must be approved before you can submit tenders.");

        // Guard: no duplicate applications
        if (await _tenderRepo.HasContractorAppliedAsync(dto.IncidentId, contractorId))
            return ServiceResult<TenderApplicationDto>.Fail(
                "You have already submitted a tender for this incident.");

        // Guard: quoted total must match sum of line items
        var lineItemsTotal = dto.LineItems.Sum(l => l.LineTotal);
        if (lineItemsTotal != dto.QuotedTotalAmount)
            return ServiceResult<TenderApplicationDto>.Fail(
                $"Quoted total ({dto.QuotedTotalAmount}) must equal the sum of line items ({lineItemsTotal}).");

        var application = new TenderApplication
        {
            IncidentId = dto.IncidentId,
            ContractorId = contractorId,
            ProposalText = dto.ProposalText.Trim(),
            EstimatedTimelineDays = dto.EstimatedTimelineDays,
            QuotedTotalAmount = dto.QuotedTotalAmount,
            Status = TenderStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
            LineItems = dto.LineItems.Select(l => new TenderLineItem
            {
                Category = l.Category,
                Description = l.Description.Trim(),
                Quantity = l.Quantity,
                UnitOfMeasure = l.UnitOfMeasure.Trim(),
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal
            }).ToList()
        };

        var id = await _tenderRepo.CreateAsync(application);

        await _audit.LogAsync("TenderSubmitted", "TenderApplication",
            id.ToString(), contractorId.ToString(),
            $"Incident: {incident.IncidentNumber}, Amount: {dto.QuotedTotalAmount:C}");

        return await GetByIdAsync(id);
    }

    // ── Update tender (Contractor — only while Submitted) ────────────────────
    public async Task<ServiceResult<TenderApplicationDto>> UpdateAsync(
        Guid applicationId, Guid contractorId, TenderApplicationUpdateDto dto)
    {
        var application = await _tenderRepo.GetByIdAsync(applicationId);
        if (application is null)
            return ServiceResult<TenderApplicationDto>.Fail("Tender application not found.");

        if (application.ContractorId != contractorId)
            return ServiceResult<TenderApplicationDto>.Fail(
                "You are not authorised to edit this tender.");

        if (application.Status != TenderStatus.Submitted)
            return ServiceResult<TenderApplicationDto>.Fail(
                "Only submitted (not yet reviewed) tenders can be updated.");

        application.ProposalText = dto.ProposalText.Trim();
        application.EstimatedTimelineDays = dto.EstimatedTimelineDays;
        application.QuotedTotalAmount = dto.QuotedTotalAmount;
        application.UpdatedAt = DateTime.UtcNow;

        await _tenderRepo.UpdateAsync(application);

        await _audit.LogAsync("TenderUpdated", "TenderApplication",
            applicationId.ToString(), contractorId.ToString(),
            $"New amount: {dto.QuotedTotalAmount:C}");

        return await GetByIdAsync(applicationId);
    }

    // ── Withdraw tender (Contractor) ──────────────────────────────────────────
    public async Task<ServiceResult> WithdrawAsync(Guid applicationId, Guid contractorId)
    {
        var application = await _tenderRepo.GetByIdAsync(applicationId);
        if (application is null)
            return ServiceResult.Fail("Tender application not found.");

        if (application.ContractorId != contractorId)
            return ServiceResult.Fail("You are not authorised to withdraw this tender.");

        if (application.Status != TenderStatus.Submitted)
            return ServiceResult.Fail("Only submitted tenders can be withdrawn.");

        application.Status = TenderStatus.Withdrawn;
        application.UpdatedAt = DateTime.UtcNow;

        await _tenderRepo.UpdateAsync(application);

        await _audit.LogAsync("TenderWithdrawn", "TenderApplication",
            applicationId.ToString(), contractorId.ToString(), "Tender withdrawn by contractor");

        return ServiceResult.Ok();
    }

    // ── Evaluate and rank all tenders for an incident (Admin) ────────────────
    // Algorithm: S = (R × 0.4) + (Cscore × 0.4) + (P × 0.2)
    // R     = AverageRating / 5.0          (normalized to 0–1)
    // Cscore = MinBid / ContractorBid      (cost efficiency,  0–1)
    // P     = (completed - late) / completed (on-time rate,   0–1)
    public async Task<ServiceResult<IReadOnlyList<TenderApplicationDto>>> EvaluateAndRankAsync(
        Guid incidentId, Guid adminId)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (incident is null)
            return ServiceResult<IReadOnlyList<TenderApplicationDto>>.Fail("Incident not found.");

        if (incident.Status != IncidentStatus.Published)
            return ServiceResult<IReadOnlyList<TenderApplicationDto>>.Fail(
                "Only published incidents can have their tenders evaluated.");

        var applications = (await _tenderRepo.GetByIncidentAsync(incidentId))
            .Where(t => t.Status == TenderStatus.Submitted)
            .ToList();

        if (applications.Count == 0)
            return ServiceResult<IReadOnlyList<TenderApplicationDto>>.Fail(
                "No submitted tenders found for this incident.");

        var minBid = applications.Min(a => a.QuotedTotalAmount);

        // Score each application
        foreach (var app in applications)
        {
            var profile = await _profileRepo.GetByUserIdAsync(app.ContractorId);

            var (rScore, cScore, pScore, weighted) =
                CalculateScore(app, minBid, profile);

            app.RatingScore = rScore;
            app.CostScore = cScore;
            app.PerformanceScore = pScore;
            app.WeightedScore = weighted;
            app.Status = TenderStatus.UnderReview;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewedByAdminId = adminId;
        }

        // Assign rank positions (1 = best / highest score)
        var ranked = applications
            .OrderByDescending(a => a.WeightedScore)
            .ToList();

        for (var i = 0; i < ranked.Count; i++)
        {
            ranked[i].RankPosition = i + 1;
            ranked[i].EvaluationNotes =
                $"Score: {ranked[i].WeightedScore:F4} " +
                $"[Rating: {ranked[i].RatingScore:F2}, " +
                $"Cost: {ranked[i].CostScore:F2}, " +
                $"Performance: {ranked[i].PerformanceScore:F2}]";
        }

        await _tenderRepo.UpdateRangeAsync(ranked);

        await _audit.LogAsync("TendersEvaluated", "Incident",
            incidentId.ToString(), adminId.ToString(),
            $"{ranked.Count} tenders evaluated and ranked");

        var dtos = ranked.Select(MapToDto).ToList();
        return ServiceResult<IReadOnlyList<TenderApplicationDto>>.Ok(dtos);
    }

    // ── Queries ───────────────────────────────────────────────────────────────
    public async Task<ServiceResult<TenderApplicationDto>> GetByIdAsync(Guid id)
    {
        var app = await _tenderRepo.GetByIdAsync(id);
        if (app is null)
            return ServiceResult<TenderApplicationDto>.Fail("Tender application not found.");
        return ServiceResult<TenderApplicationDto>.Ok(MapToDto(app));
    }

    public async Task<IReadOnlyList<TenderApplicationDto>> GetByIncidentAsync(Guid incidentId)
    {
        var apps = await _tenderRepo.GetByIncidentAsync(incidentId);
        return apps.Select(MapToDto).ToList();
    }

    public async Task<PagedResultDto<TenderApplicationListDto>> GetByContractorAsync(
        Guid contractorId, int page, int pageSize)
    {
        var items = await _tenderRepo.GetByContractorAsync(contractorId, page, pageSize);
        var total = await _tenderRepo.GetCountByContractorAsync(contractorId);
        return new PagedResultDto<TenderApplicationListDto>
        {
            Items = items.Select(MapToListDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // ── Algorithm ─────────────────────────────────────────────────────────────
    private static (decimal ratingScore, decimal costScore, decimal performanceScore, decimal weightedScore)
        CalculateScore(TenderApplication app, decimal minBid, ContractorProfile? profile)
    {
        // R: normalize 0–5 stars to 0–1
        var ratingScore = profile is not null
            ? Math.Clamp(profile.AverageRating / 5.0m, 0m, 1m)
            : 0.5m; // neutral score when profile unavailable

        // Cscore: cost efficiency — lower bid scores higher
        var costScore = app.QuotedTotalAmount > 0
            ? Math.Clamp(minBid / app.QuotedTotalAmount, 0m, 1m)
            : 0m;

        // P: on-time performance rate
        var performanceScore = profile is { CompletedJobsCount: > 0 }
            ? Math.Clamp(
                (decimal)(profile.CompletedJobsCount - profile.LateCompletionCount)
                / profile.CompletedJobsCount, 0m, 1m)
            : 1.0m; // benefit of the doubt for new contractors

        var weighted = (ratingScore * 0.4m) + (costScore * 0.4m) + (performanceScore * 0.2m);

        return (ratingScore, costScore, performanceScore, weighted);
    }

    // ── Mappers ───────────────────────────────────────────────────────────────
    private static TenderApplicationDto MapToDto(TenderApplication t) => new(
        Id: t.Id,
        IncidentId: t.IncidentId,
        ContractorId: t.ContractorId,
        ProposalText: t.ProposalText,
        EstimatedTimelineDays: t.EstimatedTimelineDays,
        QuotedTotalAmount: t.QuotedTotalAmount,
        Status: t.Status,
        SubmittedAt: t.SubmittedAt,
        WeightedScore: t.WeightedScore,
        CostScore: t.CostScore,
        RatingScore: t.RatingScore,
        PerformanceScore: t.PerformanceScore,
        RankPosition: t.RankPosition,
        EvaluationNotes: t.EvaluationNotes,
        LineItems: t.LineItems.Select(l => new TenderLineItemDto(
            l.Id, l.Category, l.Description,
            l.Quantity, l.UnitOfMeasure, l.UnitPrice, l.LineTotal))
            .ToList().AsReadOnly());

    private static TenderApplicationListDto MapToListDto(TenderApplication t) => new(
        Id: t.Id,
        IncidentId: t.IncidentId,
        ContractorId: t.ContractorId,
        Status: t.Status,
        QuotedTotalAmount: t.QuotedTotalAmount,
        WeightedScore: t.WeightedScore,
        RankPosition: t.RankPosition);
}