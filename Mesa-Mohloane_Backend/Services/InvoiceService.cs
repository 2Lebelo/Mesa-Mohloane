using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class InvoiceService : IInvoiceService
{
    private const decimal VarianceThreshold = 0.10m; // 10% flags for manual review

    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IAssignmentRepository _assignmentRepo;
    private readonly ITenderApplicationRepository _tenderRepo;
    private readonly IAuditRepository _audit;

    public InvoiceService(
        IInvoiceRepository invoiceRepo,
        IAssignmentRepository assignmentRepo,
        ITenderApplicationRepository tenderRepo,
        IAuditRepository audit)
    {
        _invoiceRepo = invoiceRepo;
        _assignmentRepo = assignmentRepo;
        _tenderRepo = tenderRepo;
        _audit = audit;
    }

    // Contractor: submit invoice
    public async Task<ServiceResult<InvoiceDto>> SubmitAsync(
        Guid contractorId, InvoiceCreateDto dto)
    {
        // Reference Check: tender application must be Approved
        var tender = await _tenderRepo.GetByIdAsync(dto.TenderApplicationId);
        if (tender is null || tender.Status != TenderStatus.Approved)
            return ServiceResult<InvoiceDto>.Fail(
                "Invoice must reference an approved tender application.");

        var assignment = await _assignmentRepo.GetByIdAsync(dto.AssignmentId);
        if (assignment is null || assignment.ContractorId != contractorId)
            return ServiceResult<InvoiceDto>.Fail("Assignment not found or access denied.");

        if (assignment.Status != AssignmentStatus.Approved)
            return ServiceResult<InvoiceDto>.Fail(
                "Work completion must be approved before an invoice can be submitted.");

        // Guard: only one invoice per assignment
        var existing = await _invoiceRepo.GetByAssignmentAsync(dto.AssignmentId);
        if (existing is not null)
            return ServiceResult<InvoiceDto>.Fail(
                "An invoice has already been submitted for this assignment.");

        // Integrity Check: line items must sum to FinalInvoiceAmount
        var lineItemsTotal = dto.LineItems.Sum(l => l.LineTotal);
        if (Math.Abs(lineItemsTotal - dto.FinalInvoiceAmount) > 0.01m)
            return ServiceResult<InvoiceDto>.Fail(
                $"Invoice total ({dto.FinalInvoiceAmount:C}) must equal " +
                $"the sum of line items ({lineItemsTotal:C}).");

        // Calculate variance against original quoted amount
        var variance = dto.OriginalQuotedAmount > 0
            ? Math.Abs(dto.FinalInvoiceAmount - dto.OriginalQuotedAmount)
              / dto.OriginalQuotedAmount
            : 0m;

        var isFlagged = variance > VarianceThreshold;

        var invoiceNumber = await _invoiceRepo.GenerateInvoiceNumberAsync();

        var invoice = new Invoice
        {
            AssignmentId = dto.AssignmentId,
            TenderApplicationId = dto.TenderApplicationId,
            ContractorId = contractorId,
            InvoiceNumber = invoiceNumber,
            OriginalQuotedAmount = dto.OriginalQuotedAmount,
            FinalInvoiceAmount = dto.FinalInvoiceAmount,
            VariancePercentage = variance * 100,
            IsVarianceFlagged = isFlagged,
            Status = isFlagged ? InvoiceStatus.Flagged : InvoiceStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
            LineItems = dto.LineItems.Select(l => new InvoiceLineItem
            {
                Category = l.Category,
                Description = l.Description.Trim(),
                Quantity = l.Quantity,
                UnitOfMeasure = l.UnitOfMeasure.Trim(),
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal
            }).ToList()
        };

        var id = await _invoiceRepo.CreateAsync(invoice);

        var flagNote = isFlagged
            ? $" — FLAGGED: {variance * 100:F1}% variance exceeds 10% threshold"
            : string.Empty;

        await _audit.LogAsync("InvoiceSubmitted", "Invoice",
            id.ToString(), contractorId.ToString(),
            $"{invoiceNumber} submitted. Amount: {dto.FinalInvoiceAmount:C}{flagNote}");

        return await GetByIdAsync(id);
    }

    // ── Admin: validate invoice ───────────────────────────────────────────────
    public async Task<ServiceResult<InvoiceDto>> ValidateAsync(
        Guid invoiceId, Guid adminId, string? remarks)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Submitted && invoice.Status != InvoiceStatus.Flagged)
            return ServiceResult<InvoiceDto>.Fail(
                "Only Submitted or Flagged invoices can be validated.");

        invoice.Status = InvoiceStatus.Validated;
        invoice.ValidatedAt = DateTime.UtcNow;
        invoice.ValidationRemarks = remarks?.Trim();
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepo.UpdateAsync(invoice);

        await _audit.LogAsync("InvoiceValidated", "Invoice",
            invoiceId.ToString(), adminId.ToString(),
            $"Invoice {invoice.InvoiceNumber} validated by admin");

        return await GetByIdAsync(invoiceId);
    }

    // ── Admin: approve invoice ────────────────────────────────────────────────
    public async Task<ServiceResult<InvoiceDto>> ApproveAsync(Guid invoiceId, Guid adminId)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Validated)
            return ServiceResult<InvoiceDto>.Fail(
                "Invoice must be validated before it can be approved.");

        invoice.Status = InvoiceStatus.Approved;
        invoice.ApprovedAt = DateTime.UtcNow;
        invoice.ApprovedByAdminId = adminId;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepo.UpdateAsync(invoice);

        await _audit.LogAsync("InvoiceApproved", "Invoice",
            invoiceId.ToString(), adminId.ToString(),
            $"Invoice {invoice.InvoiceNumber} approved for payment");

        return await GetByIdAsync(invoiceId);
    }

    // ── Admin: reject invoice ─────────────────────────────────────────────────
    public async Task<ServiceResult<InvoiceDto>> RejectAsync(
        Guid invoiceId, Guid adminId, string remarks)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("Invoice not found.");

        invoice.Status = InvoiceStatus.Rejected;
        invoice.ValidationRemarks = remarks.Trim();
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepo.UpdateAsync(invoice);

        await _audit.LogAsync("InvoiceRejected", "Invoice",
            invoiceId.ToString(), adminId.ToString(),
            $"Invoice {invoice.InvoiceNumber} rejected: {remarks}");

        return await GetByIdAsync(invoiceId);
    }

    // ── Citizen: acknowledge invoice ──────────────────────────────────────────
    public async Task<ServiceResult<InvoiceDto>> AcknowledgeAsync(Guid invoiceId, Guid citizenId)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Approved)
            return ServiceResult<InvoiceDto>.Fail(
                "Invoice must be approved by admin before citizen acknowledgement.");

        if (invoice.CitizenAcknowledgedAt.HasValue)
            return ServiceResult<InvoiceDto>.Fail(
                "You have already acknowledged this invoice.");

        invoice.CitizenAcknowledgedAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepo.UpdateAsync(invoice);

        await _audit.LogAsync("InvoiceAcknowledgedByCitizen", "Invoice",
            invoiceId.ToString(), citizenId.ToString(),
            $"Invoice {invoice.InvoiceNumber} acknowledged by citizen");

        return await GetByIdAsync(invoiceId);
    }

    // ── Queries ───────────────────────────────────────────────────────────────
    public async Task<ServiceResult<InvoiceDto>> GetByIdAsync(Guid id)
    {
        var i = await _invoiceRepo.GetByIdAsync(id);
        if (i is null) return ServiceResult<InvoiceDto>.Fail("Invoice not found.");
        return ServiceResult<InvoiceDto>.Ok(MapToDto(i));
    }

    public async Task<ServiceResult<InvoiceDto>> GetByAssignmentAsync(Guid assignmentId)
    {
        var i = await _invoiceRepo.GetByAssignmentAsync(assignmentId);
        if (i is null) return ServiceResult<InvoiceDto>.Fail("No invoice found for this assignment.");
        return ServiceResult<InvoiceDto>.Ok(MapToDto(i));
    }

    public async Task<PagedResultDto<InvoiceListDto>> GetByContractorAsync(
        Guid contractorId, int page, int pageSize)
    {
        var items = await _invoiceRepo.GetByContractorAsync(contractorId, page, pageSize);
        var total = await _invoiceRepo.GetCountByContractorAsync(contractorId);
        return new PagedResultDto<InvoiceListDto>
        {
            Items = items.Select(MapToListDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultDto<InvoiceListDto>> GetFlaggedAsync(int page, int pageSize)
    {
        var items = await _invoiceRepo.GetFlaggedAsync(page, pageSize);
        return new PagedResultDto<InvoiceListDto>
        {
            Items = items.Select(MapToListDto).ToList(),
            TotalCount = items.Count(),
            Page = page,
            PageSize = pageSize
        };
    }

    private static InvoiceDto MapToDto(Invoice i) => new(
        i.Id, i.AssignmentId, i.TenderApplicationId, i.ContractorId,
        i.InvoiceNumber, i.OriginalQuotedAmount, i.FinalInvoiceAmount,
        i.VariancePercentage, i.IsVarianceFlagged, i.Status, i.SubmittedAt,
        i.ValidatedAt, i.ApprovedAt, i.ApprovedByAdminId, i.DisbursedAt,
        i.CitizenAcknowledgedAt, i.ValidationRemarks,
        i.LineItems.Select(l => new InvoiceLineItemDto(
            l.Id, l.Category, l.Description,
            l.Quantity, l.UnitOfMeasure, l.UnitPrice, l.LineTotal))
            .ToList().AsReadOnly());

    private static InvoiceListDto MapToListDto(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.FinalInvoiceAmount, i.Status, i.SubmittedAt);
}