using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IAssignmentRepository _assignmentRepo;
    private readonly IIncidentRepository _incidentRepo;
    private readonly IAuditRepository _audit;

    public PaymentService(
        IPaymentRepository paymentRepo,
        IInvoiceRepository invoiceRepo,
        IAssignmentRepository assignmentRepo,
        IIncidentRepository incidentRepo,
        IAuditRepository audit)
    {
        _paymentRepo = paymentRepo;
        _invoiceRepo = invoiceRepo;
        _assignmentRepo = assignmentRepo;
        _incidentRepo = incidentRepo;
        _audit = audit;
    }

    // ── Admin: initiate payment ───────────────────────────────────────────────
    public async Task<ServiceResult<PaymentDto>> InitiateAsync(Guid adminId, PaymentCreateDto dto)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(dto.InvoiceId);
        if (invoice is null)
            return ServiceResult<PaymentDto>.Fail("Invoice not found.");

        // Status Guard: invoice must be Approved
        if (invoice.Status != InvoiceStatus.Approved)
            return ServiceResult<PaymentDto>.Fail(
                "Payment can only be initiated for an Approved invoice.");

        // Status Guard: citizen must have acknowledged the invoice
        if (!invoice.CitizenAcknowledgedAt.HasValue)
            return ServiceResult<PaymentDto>.Fail(
                "Citizen must acknowledge the invoice before payment can be initiated.");

        // Status Guard: admin must have approved the work completion
        var assignment = await _assignmentRepo.GetByIdAsync(invoice.AssignmentId);
        if (assignment is null || !assignment.AdminApprovedAt.HasValue)
            return ServiceResult<PaymentDto>.Fail(
                "Admin must approve the work completion before payment can be initiated.");

        // Guard: no duplicate payment
        var existing = await _paymentRepo.GetByInvoiceAsync(dto.InvoiceId);
        if (existing is not null)
            return ServiceResult<PaymentDto>.Fail(
                "A payment has already been initiated for this invoice.");

        var payment = new Payment
        {
            InvoiceId = dto.InvoiceId,
            Amount = dto.Amount,
            PaymentReference = dto.PaymentReference.Trim(),
            Method = dto.Method.Trim(),
            Status = PaymentStatus.Initiated,
            InitiatedAt = DateTime.UtcNow
        };

        var id = await _paymentRepo.CreateAsync(payment);

        await _audit.LogAsync("PaymentInitiated", "Payment",
            id.ToString(), adminId.ToString(),
            $"Payment of {dto.Amount:C} initiated for Invoice {invoice.InvoiceNumber}");

        return await GetByIdAsync(id);
    }

    // ── Admin: approve payment ────────────────────────────────────────────────
    public async Task<ServiceResult<PaymentDto>> ApproveAsync(Guid paymentId, Guid adminId)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId);
        if (payment is null)
            return ServiceResult<PaymentDto>.Fail("Payment not found.");

        if (payment.Status != PaymentStatus.Initiated)
            return ServiceResult<PaymentDto>.Fail(
                "Only Initiated payments can be approved.");

        payment.Status = PaymentStatus.Approved;
        payment.ApprovedAt = DateTime.UtcNow;
        payment.ApprovedByAdminId = adminId;
        payment.UpdatedAt = DateTime.UtcNow;

        await _paymentRepo.UpdateAsync(payment);

        await _audit.LogAsync("PaymentApproved", "Payment",
            paymentId.ToString(), adminId.ToString(),
            $"Payment of {payment.Amount:C} approved");

        return await GetByIdAsync(paymentId);
    }

    // ── Admin: disburse payment ───────────────────────────────────────────────
    public async Task<ServiceResult<PaymentDto>> DisburseAsync(Guid paymentId, Guid adminId)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId);
        if (payment is null)
            return ServiceResult<PaymentDto>.Fail("Payment not found.");

        if (payment.Status != PaymentStatus.Approved)
            return ServiceResult<PaymentDto>.Fail(
                "Payment must be Approved before disbursement.");

        payment.Status = PaymentStatus.Disbursed;
        payment.DisbursedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        await _paymentRepo.UpdateAsync(payment);

        // Mark invoice as Disbursed and update incident to Closed
        var invoice = await _invoiceRepo.GetByIdAsync(payment.InvoiceId);
        if (invoice is not null)
        {
            invoice.Status = InvoiceStatus.Disbursed;
            invoice.DisbursedAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _invoiceRepo.UpdateAsync(invoice);

            var assignment = await _assignmentRepo.GetByIdAsync(invoice.AssignmentId);
            if (assignment is not null)
            {
                assignment.Status = AssignmentStatus.Closed;
                assignment.UpdatedAt = DateTime.UtcNow;
                await _assignmentRepo.UpdateAsync(assignment);

                var incident = await _incidentRepo.GetByIdAsync(assignment.IncidentId);
                if (incident is not null)
                {
                    incident.Status = IncidentStatus.Closed;
                    incident.ClosedAt = DateTime.UtcNow;
                    incident.UpdatedAt = DateTime.UtcNow;
                    await _incidentRepo.UpdateAsync(incident);
                }
            }
        }

        await _audit.LogAsync("PaymentDisbursed", "Payment",
            paymentId.ToString(), adminId.ToString(),
            $"Payment of {payment.Amount:C} disbursed to contractor");

        return await GetByIdAsync(paymentId);
    }

    // ── Admin: mark payment failed ────────────────────────────────────────────
    public async Task<ServiceResult<PaymentDto>> MarkFailedAsync(
        Guid paymentId, Guid adminId, string reason)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId);
        if (payment is null)
            return ServiceResult<PaymentDto>.Fail("Payment not found.");

        payment.Status = PaymentStatus.Failed;
        payment.FailureReason = reason.Trim();
        payment.UpdatedAt = DateTime.UtcNow;

        await _paymentRepo.UpdateAsync(payment);

        await _audit.LogAsync("PaymentFailed", "Payment",
            paymentId.ToString(), adminId.ToString(),
            $"Payment failed: {reason}");

        return await GetByIdAsync(paymentId);
    }

    // ── Queries ───────────────────────────────────────────────────────────────
    public async Task<ServiceResult<PaymentDto>> GetByIdAsync(Guid id)
    {
        var p = await _paymentRepo.GetByIdAsync(id);
        if (p is null) return ServiceResult<PaymentDto>.Fail("Payment not found.");
        return ServiceResult<PaymentDto>.Ok(MapToDto(p));
    }

    public async Task<ServiceResult<PaymentDto>> GetByInvoiceAsync(Guid invoiceId)
    {
        var p = await _paymentRepo.GetByInvoiceAsync(invoiceId);
        if (p is null) return ServiceResult<PaymentDto>.Fail("No payment found for this invoice.");
        return ServiceResult<PaymentDto>.Ok(MapToDto(p));
    }

    private static PaymentDto MapToDto(Payment p) => new(
        p.Id, p.InvoiceId, p.Amount, p.PaymentReference, p.Method,
        p.Status, p.InitiatedAt, p.ApprovedAt, p.ApprovedByAdminId,
        p.DisbursedAt, p.FailureReason);
}