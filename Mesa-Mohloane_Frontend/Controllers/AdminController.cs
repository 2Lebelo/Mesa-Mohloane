using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Frontend.Dtos;
using Mesa_Mohloane_Frontend.Services.Api;
using Mesa_Mohloane_Frontend.ViewModels;

namespace Mesa_Mohloane_Frontend.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Administrator")]
public sealed class AdminController : BaseController
{
    private readonly IIncidentApiService _incidents;
    private readonly ITenderApiService _tenders;
    private readonly IAssignmentApiService _assignments;
    private readonly IInvoiceApiService _invoices;
    private readonly IPaymentApiService _payments;
    private readonly IUserApiService _users;
    private readonly INotificationApiService _notifications;
    private readonly IContractorProfileApiService _contractorProfiles;
    public AdminController(
        IHttpClientFactory http,
        IConfiguration config,
        IIncidentApiService incidents,
        ITenderApiService tenders,
        IAssignmentApiService assignments,
        IInvoiceApiService invoices,
        IPaymentApiService payments,
        IUserApiService users,
        INotificationApiService notifications,
        IContractorProfileApiService contractorProfiles)
        : base(http, config)
    {
        _incidents = incidents;
        _tenders = tenders;
        _assignments = assignments;
        _invoices = invoices;
        _payments = payments;
        _users = users;
        _notifications = notifications;
        _contractorProfiles = contractorProfiles;
    }

    [HttpGet]
    public IActionResult Dashboard()
    {
        SetUserViewData();
        ViewData["Title"] = "Admin Dashboard";
        ViewData["ActiveNav"] = "Dashboard";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Incidents(int page = 1, int? status = null, string? search = null)
    {
        SetUserViewData();
        ViewData["Title"] = "All Incidents";
        ViewData["ActiveNav"] = "Incidents";
        ViewData["Status"] = status;
        ViewData["Search"] = search;
        var model = await _incidents.GetAllAsync(page, 10, status, search);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> IncidentDetails(Guid id)
    {
        SetUserViewData();
        ViewData["Title"] = "Incident Details";
        ViewData["ActiveNav"] = "Incidents";

        var incident = await _incidents.GetByIdAsync(id);
        if (incident is null) return NotFound();

        var tenders = await _tenders.GetByIncidentAsync(id) ?? Array.Empty<TenderApplicationDto>();
        return View(new AdminIncidentDetailViewModel
        {
            Incident = incident,
            Tenders = tenders
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyIncident(Guid id)
    {
        var (ok, _, error) = await _incidents.VerifyAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "Incident verified." : error ?? "Failed to verify incident.";
        return RedirectToAction(nameof(IncidentDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectIncident(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Rejection reason is required.";
            return RedirectToAction(nameof(IncidentDetails), new { id });
        }

        var (ok, _, error) = await _incidents.RejectAsync(id, reason);
        TempData[ok ? "Success" : "Error"] = ok ? "Incident rejected." : error ?? "Failed to reject incident.";
        return RedirectToAction(nameof(IncidentDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishIncident(Guid id)
    {
        var (ok, _, error) = await _incidents.PublishAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "Incident published." : error ?? "Failed to publish incident.";
        return RedirectToAction(nameof(IncidentDetails), new { id });
    }

    [HttpGet]
    public IActionResult TenderApplications()
    {
        SetUserViewData();
        ViewData["Title"] = "Tender Applications";
        ViewData["ActiveNav"] = "Tenders";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> TenderDetails(Guid id)
    {
        SetUserViewData();
        ViewData["Title"] = "Tender Details";
        ViewData["ActiveNav"] = "Tenders";

        if (id == Guid.Empty) return BadRequest();

        var tender = await _tenders.GetByIdAsync(id);
        if (tender is null) return NotFound();

        return View(tender);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EvaluateTenders(Guid incidentId)
    {
        var (ok, _, error) = await _tenders.EvaluateAsync(incidentId);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Tenders evaluated and ranked successfully."
            : error ?? "Failed to evaluate tenders.";

        return RedirectToAction(nameof(IncidentDetails), new { id = incidentId });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTender(Guid incidentId, Guid tenderId)
    {
        //var (ok, _, error) = await _tenders.AssignTenderAsync(incidentId, tenderId);
        var (ok, _, error) = await _assignments.AssignAsync(incidentId, tenderId);
        TempData[ok ? "Success" : "Error"] = ok ? "Contractor assigned." : error ?? "Failed to assign contractor.";
        return RedirectToAction(nameof(IncidentDetails), new { id = incidentId });
    }

    [HttpGet]
    public async Task<IActionResult> Assignments(int page = 1, int? status = null)
    {
        SetUserViewData();
        ViewData["Title"] = "Assignments";
        ViewData["ActiveNav"] = "Assignments";
        ViewData["Status"] = status;

        var model = await _assignments.GetAllAsync(page, 10, status);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignmentDetails(Guid id)
    {
        SetUserViewData();
        ViewData["Title"] = "Assignment Details";
        ViewData["ActiveNav"] = "Assignments";

        if (id == Guid.Empty)
            return BadRequest();

        var assignment = await _assignments.GetByIdAsync(id);

        if (assignment is null)
            return NotFound();

        return View(assignment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAssignment(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "Invalid assignment id.";
            return RedirectToAction(nameof(Assignments));
        }

        var (ok, _, error) = await _assignments.ApproveAsync(id);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Assignment completion approved successfully."
            : error ?? "Failed to approve assignment completion.";

        return RedirectToAction(nameof(AssignmentDetails), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Invoices(int page = 1, int? status = null)
    {
        SetUserViewData();
        ViewData["Title"] = "Invoices";
        ViewData["ActiveNav"] = "Invoices";
        ViewData["Status"] = status;
        var model = await _invoices.GetAllAsync(page, 10, status);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> InvoiceDetails(Guid id)
    {
        SetUserViewData();
        ViewData["Title"] = "Invoice Review";
        ViewData["ActiveNav"] = "Invoices";
        var invoice = await _invoices.GetByIdAsync(id);
        if (invoice is null) return NotFound();
        return View(invoice);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateInvoice(Guid id, string? remarks)
    {
        var (ok, _, error) = await _invoices.ValidateAsync(id, remarks);
        TempData[ok ? "Success" : "Error"] = ok ? "Invoice validated." : error ?? "Failed to validate invoice.";
        return RedirectToAction(nameof(InvoiceDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveInvoice(Guid id)
    {
        var (ok, _, error) = await _invoices.ApproveAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "Invoice approved." : error ?? "Failed to approve invoice.";
        return RedirectToAction(nameof(InvoiceDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectInvoice(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Rejection reason is required.";
            return RedirectToAction(nameof(InvoiceDetails), new { id });
        }
        var (ok, _, error) = await _invoices.RejectAsync(id, reason);
        TempData[ok ? "Success" : "Error"] = ok ? "Invoice rejected." : error ?? "Failed to reject invoice.";
        return RedirectToAction(nameof(InvoiceDetails), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Payments(Guid? invoiceId = null, Guid? paymentId = null)
    {
        SetUserViewData();
        ViewData["Title"] = "Payments";
        ViewData["ActiveNav"] = "Payments";

        PaymentDto? payment = null;
        InvoiceDto? invoice = null;

        if (paymentId.HasValue && paymentId.Value != Guid.Empty)
        {
            payment = await _payments.GetByIdAsync(paymentId.Value);

            if (payment is not null)
                invoice = await _invoices.GetByIdAsync(payment.InvoiceId);
        }
        else if (invoiceId.HasValue && invoiceId.Value != Guid.Empty)
        {
            invoice = await _invoices.GetByIdAsync(invoiceId.Value);
            payment = await _payments.GetByInvoiceAsync(invoiceId.Value);
        }

        var model = new PaymentReviewViewModel
        {
            InvoiceId = invoice?.Id ?? invoiceId,
            Invoice = invoice,
            Payment = payment
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InitiatePayment(PaymentCreateDto dto)
    {
        if (dto.InvoiceId == Guid.Empty)
        {
            TempData["Error"] = "Invalid invoice id.";
            return RedirectToAction(nameof(Payments));
        }

        if (dto.Amount <= 0)
        {
            TempData["Error"] = "Payment amount must be greater than zero.";
            return RedirectToAction(nameof(Payments), new { invoiceId = dto.InvoiceId });
        }

        if (string.IsNullOrWhiteSpace(dto.PaymentReference))
        {
            TempData["Error"] = "Payment reference is required.";
            return RedirectToAction(nameof(Payments), new { invoiceId = dto.InvoiceId });
        }

        if (string.IsNullOrWhiteSpace(dto.Method))
        {
            TempData["Error"] = "Payment method is required.";
            return RedirectToAction(nameof(Payments), new { invoiceId = dto.InvoiceId });
        }

        var cleanDto = dto with
        {
            PaymentReference = dto.PaymentReference.Trim(),
            Method = dto.Method.Trim()
        };

        var (ok, payment, error) = await _payments.InitiateAsync(cleanDto);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Payment initiated successfully."
            : error ?? "Failed to initiate payment.";

        return RedirectToAction(
            nameof(Payments),
            new
            {
                invoiceId = dto.InvoiceId,
                paymentId = payment?.Id
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePayment(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "Invalid payment id.";
            return RedirectToAction(nameof(Payments));
        }

        var paymentBefore = await _payments.GetByIdAsync(id);
        var (ok, payment, error) = await _payments.ApproveAsync(id);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Payment approved successfully."
            : error ?? "Failed to approve payment.";

        return RedirectToAction(
            nameof(Payments),
            new
            {
                invoiceId = payment?.InvoiceId ?? paymentBefore?.InvoiceId,
                paymentId = id
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisbursePayment(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "Invalid payment id.";
            return RedirectToAction(nameof(Payments));
        }

        var paymentBefore = await _payments.GetByIdAsync(id);
        var (ok, payment, error) = await _payments.DisburseAsync(id);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Payment disbursed successfully. Related invoice, assignment, and incident were closed automatically."
            : error ?? "Failed to disburse payment.";

        return RedirectToAction(
            nameof(Payments),
            new
            {
                invoiceId = payment?.InvoiceId ?? paymentBefore?.InvoiceId,
                paymentId = id
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FailPayment(Guid id, string reason)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "Invalid payment id.";
            return RedirectToAction(nameof(Payments));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Failure reason is required.";
            return RedirectToAction(nameof(Payments), new { paymentId = id });
        }

        var paymentBefore = await _payments.GetByIdAsync(id);
        var (ok, payment, error) = await _payments.MarkFailedAsync(id, reason);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Payment marked as failed."
            : error ?? "Failed to mark payment as failed.";

        return RedirectToAction(
            nameof(Payments),
            new
            {
                invoiceId = payment?.InvoiceId ?? paymentBefore?.InvoiceId,
                paymentId = id
            });
    }

    [HttpGet]
    public async Task<IActionResult> Users(int page = 1, string? search = null)
    {
        SetUserViewData();
        ViewData["Title"] = "Users";
        ViewData["ActiveNav"] = "Users";
        ViewData["Search"] = search;
        var model = await _users.GetAllAsync(page, 10, search);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUser(Guid id)
    {
        var (ok, error) = await _users.ToggleActiveAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "User status updated." : error ?? "Failed to update user.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> Notifications(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "Notifications";
        ViewData["ActiveNav"] = "Notifications";
        var model = await _notifications.GetMineAsync(page, 20, false);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(Guid id)
    {
        await _notifications.MarkAsReadAsync(id);
        return RedirectToAction(nameof(Notifications));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        await _notifications.MarkAllAsReadAsync();
        return RedirectToAction(nameof(Notifications));
    }


    //[HttpGet]
    //public async Task<IActionResult> Contractors(int page = 1)
    //{
    //    SetUserViewData();
    //    ViewData["Title"] = "Contractor Profiles";
    //    ViewData["ActiveNav"] = "Contractors";

    //    var model = await _contractorProfiles.GetAllApprovedAsync(page, 10);
    //    return View(model);
    //}

    [HttpGet]
    public async Task<IActionResult> Contractors(int page = 1, bool? isApproved = null)
    {
        SetUserViewData();
        ViewData["Title"] = "Contractor Profiles";
        ViewData["ActiveNav"] = "Contractors";
        ViewData["IsApproved"] = isApproved;

        var model = await _contractorProfiles.GetAllAsync(page, 10, isApproved);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveContractorProfile(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "Invalid contractor profile id.";
            return RedirectToAction(nameof(Contractors));
        }

        var (ok, _, error) = await _contractorProfiles.ApproveAsync(id);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Contractor profile approved successfully."
            : error ?? "Failed to approve contractor profile.";

        return RedirectToAction(nameof(Contractors));
    }

    [HttpGet]
    public async Task<IActionResult> ContractorDetails(Guid id)
    {
        SetUserViewData();

        ViewData["Title"] = "Contractor Details";
        ViewData["ActiveNav"] = "Contractors";

        if (id == Guid.Empty)
            return BadRequest();

        var contractor = await _contractorProfiles.GetByIdAsync(id);

        if (contractor is null)
        {
            TempData["Error"] = "Contractor profile could not be found.";
            return RedirectToAction(nameof(Contractors));
        }

        return View(contractor);
    }
}
