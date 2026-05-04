using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class InvoiceService : IInvoiceService
{
    private const decimal VarianceThreshold = 0.10m;

    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IAssignmentRepository _assignmentRepo;
    private readonly ITenderApplicationRepository _tenderRepo;
    private readonly IIncidentRepository _incidentRepo;
    private readonly IAuditRepository _audit;
    private readonly INotificationService _notifications;
    private readonly IUserRepository _users;

    public InvoiceService(
        IInvoiceRepository invoiceRepo,
        IAssignmentRepository assignmentRepo,
        ITenderApplicationRepository tenderRepo,
        IIncidentRepository incidentRepo,
        IAuditRepository audit,
        INotificationService notifications,
        IUserRepository users)
    {
        _invoiceRepo = invoiceRepo;
        _assignmentRepo = assignmentRepo;
        _tenderRepo = tenderRepo;
        _incidentRepo = incidentRepo;
        _audit = audit;
        _notifications = notifications;
        _users = users;
    }

    public async Task<ServiceResult<InvoiceDto>> SubmitAsync(
        Guid contractorId,
        InvoiceCreateDto dto)
    {
        var tender = await _tenderRepo.GetByIdAsync(dto.TenderApplicationId);

        if (tender is null || tender.Status != TenderStatus.Approved)
            return ServiceResult<InvoiceDto>.Fail(
                "Invoice must reference an approved tender application.");

        var assignment = await _assignmentRepo.GetByIdAsync(dto.AssignmentId);

        if (assignment is null || assignment.ContractorId != contractorId)
            return ServiceResult<InvoiceDto>.Fail("Assignment not found or access denied.");

        if (assignment.TenderApplicationId != dto.TenderApplicationId)
            return ServiceResult<InvoiceDto>.Fail(
                "Invoice tender reference does not match the assigned tender.");

        if (assignment.Status != AssignmentStatus.Approved)
            return ServiceResult<InvoiceDto>.Fail(
                "Work completion must be approved before an invoice can be submitted.");

        var existing = await _invoiceRepo.GetByAssignmentAsync(dto.AssignmentId);

        if (existing is not null)
            return ServiceResult<InvoiceDto>.Fail(
                "An invoice has already been submitted for this assignment.");

        if (dto.OriginalQuotedAmount != tender.QuotedTotalAmount)
            return ServiceResult<InvoiceDto>.Fail(
                "Original quoted amount must match the approved tender quotation.");

        if (dto.LineItems is null || !dto.LineItems.Any())
            return ServiceResult<InvoiceDto>.Fail(
                "Invoice must include at least one line item.");

        var lineItemsTotal = dto.LineItems.Sum(l => l.LineTotal);

        if (Math.Abs(lineItemsTotal - dto.FinalInvoiceAmount) > 0.01m)
            return ServiceResult<InvoiceDto>.Fail(
                $"Invoice total ({dto.FinalInvoiceAmount:C}) must equal the sum of line items ({lineItemsTotal:C}).");

        var variance = dto.OriginalQuotedAmount > 0
            ? Math.Abs(dto.FinalInvoiceAmount - dto.OriginalQuotedAmount) / dto.OriginalQuotedAmount
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

        await _audit.LogAsync(
            "InvoiceSubmitted",
            "Invoice",
            id.ToString(),
            contractorId.ToString(),
            $"{invoiceNumber} submitted. Amount: {dto.FinalInvoiceAmount:C}{flagNote}");

        await NotifyAdministratorsAsync(
            NotificationType.InvoiceSubmitted,
            "Invoice submitted",
            $"Invoice {invoiceNumber} submitted for review.",
            id);

        if (isFlagged)
        {
            await NotifyAuditorsAsync(
                NotificationType.InvoiceFlagged,
                "Invoice flagged for review",
                $"Invoice {invoiceNumber} exceeded the 10% variance threshold.",
                id);
        }

        return await GetByIdAsync(id);
    }

    public async Task<ServiceResult<InvoiceDto>> ValidateAsync(
        Guid invoiceId,
        Guid adminId,
        string? remarks)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);

        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Submitted &&
            invoice.Status != InvoiceStatus.Flagged)
        {
            return ServiceResult<InvoiceDto>.Fail(
                "Only Submitted or Flagged invoices can be validated.");
        }

        invoice.Status = InvoiceStatus.Validated;
        invoice.ValidatedAt = DateTime.UtcNow;
        invoice.ValidationRemarks = remarks?.Trim();
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepo.UpdateAsync(invoice);

        await _audit.LogAsync(
            "InvoiceValidated",
            "Invoice",
            invoiceId.ToString(),
            adminId.ToString(),
            $"Invoice {invoice.InvoiceNumber} validated");

        await _notifications.SendAsync(
            invoice.ContractorId,
            NotificationType.InvoiceSubmitted,
            "Invoice validated",
            $"Invoice {invoice.InvoiceNumber} has been validated.",
            "Invoice",
            invoiceId);

        return await GetByIdAsync(invoiceId);
    }

    public async Task<ServiceResult<InvoiceDto>> ApproveAsync(Guid invoiceId, Guid adminId)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);

        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Validated)
            return ServiceResult<InvoiceDto>.Fail(
                "Invoice must be validated before it can be approved.");

        var now = DateTime.UtcNow;

        invoice.Status = InvoiceStatus.Approved;
        invoice.ApprovedAt = now;
        invoice.ApprovedByAdminId = adminId;

        // Business rule change:
        // Citizen is no longer involved in invoice acknowledgement.
        // Approval by Admin/Auditor automatically satisfies the acknowledgement gate.
        invoice.CitizenAcknowledgedAt ??= now;

        invoice.UpdatedAt = now;

        await _invoiceRepo.UpdateAsync(invoice);

        await _audit.LogAsync(
            "InvoiceApproved",
            "Invoice",
            invoiceId.ToString(),
            adminId.ToString(),
            $"Invoice {invoice.InvoiceNumber} approved and automatically acknowledged for payment eligibility");

        await _notifications.SendAsync(
            invoice.ContractorId,
            NotificationType.InvoiceApproved,
            "Invoice approved",
            $"Invoice {invoice.InvoiceNumber} has been approved.",
            "Invoice",
            invoiceId);

        await NotifyAdministratorsAsync(
            NotificationType.InvoiceApproved,
            "Invoice approved",
            $"Invoice {invoice.InvoiceNumber} was approved and automatically acknowledged.",
            invoiceId);

        return await GetByIdAsync(invoiceId);
    }

    public async Task<ServiceResult<InvoiceDto>> RejectAsync(
        Guid invoiceId,
        Guid adminId,
        string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
            return ServiceResult<InvoiceDto>.Fail("Rejection reason is required.");

        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);

        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("Invoice not found.");

        if (invoice.Status == InvoiceStatus.Approved ||
            invoice.Status == InvoiceStatus.Disbursed)
        {
            return ServiceResult<InvoiceDto>.Fail(
                "Approved or disbursed invoices cannot be rejected.");
        }

        invoice.Status = InvoiceStatus.Rejected;
        invoice.ValidationRemarks = remarks.Trim();
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepo.UpdateAsync(invoice);

        await _audit.LogAsync(
            "InvoiceRejected",
            "Invoice",
            invoiceId.ToString(),
            adminId.ToString(),
            $"Invoice {invoice.InvoiceNumber} rejected: {remarks}");

        await _notifications.SendAsync(
            invoice.ContractorId,
            NotificationType.InvoiceRejected,
            "Invoice rejected",
            $"Invoice {invoice.InvoiceNumber} was rejected: {remarks}",
            "Invoice",
            invoiceId);

        return await GetByIdAsync(invoiceId);
    }

    public async Task<ServiceResult<InvoiceDto>> GetByIdAsync(Guid id)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(id);

        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("Invoice not found.");

        return ServiceResult<InvoiceDto>.Ok(MapToDto(invoice));
    }

    public async Task<ServiceResult<InvoiceDto>> GetByAssignmentAsync(Guid assignmentId)
    {
        var invoice = await _invoiceRepo.GetByAssignmentAsync(assignmentId);

        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("No invoice found for this assignment.");

        return ServiceResult<InvoiceDto>.Ok(MapToDto(invoice));
    }

    public async Task<PagedResultDto<InvoiceListDto>> GetByContractorAsync(
        Guid contractorId,
        int page,
        int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? 10 : pageSize;

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
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var items = await _invoiceRepo.GetFlaggedAsync(page, pageSize);

        return new PagedResultDto<InvoiceListDto>
        {
            Items = items.Select(MapToListDto).ToList(),
            TotalCount = items.Count(),
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultDto<InvoiceListDto>> GetAllAsync(
        int page,
        int pageSize,
        InvoiceStatus? status)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var items = await _invoiceRepo.GetAllAsync(page, pageSize, status);
        var total = await _invoiceRepo.GetTotalCountAsync(status);

        return new PagedResultDto<InvoiceListDto>
        {
            Items = items.Select(MapToListDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static InvoiceDto MapToDto(Invoice invoice) => new(
        invoice.Id,
        invoice.AssignmentId,
        invoice.TenderApplicationId,
        invoice.ContractorId,
        invoice.InvoiceNumber,
        invoice.OriginalQuotedAmount,
        invoice.FinalInvoiceAmount,
        invoice.VariancePercentage,
        invoice.IsVarianceFlagged,
        invoice.Status,
        invoice.SubmittedAt,
        invoice.ValidatedAt,
        invoice.ApprovedAt,
        invoice.ApprovedByAdminId,
        invoice.DisbursedAt,
        invoice.CitizenAcknowledgedAt,
        invoice.ValidationRemarks,
        invoice.LineItems.Select(line => new InvoiceLineItemDto(
            line.Id,
            line.Category,
            line.Description,
            line.Quantity,
            line.UnitOfMeasure,
            line.UnitPrice,
            line.LineTotal))
            .ToList()
            .AsReadOnly());

    private static InvoiceListDto MapToListDto(Invoice invoice) => new(
        invoice.Id,
        invoice.InvoiceNumber,
        invoice.FinalInvoiceAmount,
        invoice.Status,
        invoice.SubmittedAt);

    private async Task NotifyAdministratorsAsync(
        NotificationType type,
        string title,
        string message,
        Guid invoiceId)
    {
        var admins = await _users.GetAdministratorsAsync(1, 200, null);

        foreach (var admin in admins)
        {
            await _notifications.SendAsync(
                admin.Id,
                type,
                title,
                message,
                "Invoice",
                invoiceId);
        }
    }

    private async Task NotifyAuditorsAsync(
        NotificationType type,
        string title,
        string message,
        Guid invoiceId)
    {
        var auditors = await _users.GetAuditorsAsync(1, 200, null);

        foreach (var auditor in auditors)
        {
            await _notifications.SendAsync(
                auditor.Id,
                type,
                title,
                message,
                "Invoice",
                invoiceId);
        }
    }
}