using Mesa_Mohloane_Frontend.Dtos;
using Mesa_Mohloane_Frontend.Services.Api;
using Mesa_Mohloane_Frontend.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mesa_Mohloane_Frontend.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Inspector,Auditor")]
public sealed class InspectorController : BaseController
{
    private readonly IAuditLogApiService _auditLogs;
    private readonly IInvoiceApiService _invoices;
    private readonly IIncidentApiService _incidents;
    private readonly INotificationApiService _notifications;
    private readonly IContractorProfileApiService _contractorProfiles;
    private readonly ITenderApiService _tenders;
    public InspectorController(
        IHttpClientFactory http,
        IConfiguration config,
        IAuditLogApiService auditLogs,
        IInvoiceApiService invoices,
        IIncidentApiService incidents,
        IContractorProfileApiService contractorProfiles,
        INotificationApiService notifications,
        ITenderApiService tenders)
        : base(http, config)
    {
        _auditLogs = auditLogs;
        _invoices = invoices;
        _incidents = incidents;
        _notifications = notifications;
        _contractorProfiles = contractorProfiles;
        _tenders = tenders;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        SetUserViewData();
        ViewData["Title"] = "Inspector Dashboard";
        ViewData["ActiveNav"] = "Dashboard";

        var model = new InspectorDashboardViewModel
        {
            RecentAuditLogs = await _auditLogs.GetAllAsync(1, 8),
            FlaggedInvoices = await _invoices.GetFlaggedAsync(1, 5),
            Notifications = await _notifications.GetMineAsync(1, 5, true),
            UnreadNotifications = await _notifications.GetUnreadCountAsync()
        };

        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> AuditLogs(
        int page = 1,
        string? entityName = null,
        string? actionType = null,
        Guid? actorUserId = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        SetUserViewData();

        ViewData["Title"] = "Audit Logs";
        ViewData["ActiveNav"] = "AuditLogs";

        page = Math.Max(page, 1);

        var logs = await _auditLogs.GetAllAsync(
            page,
            20,
            entityName,
            actionType,
            actorUserId,
            from,
            to);

        var model = new AuditLogListViewModel
        {
            EntityName = entityName,
            ActionType = actionType,
            ActorUserId = actorUserId,
            From = from,
            To = to,
            Logs = logs ?? new AuditLogPagedResultDto
            {
                Items = new List<AuditLogDto>(),
                Page = page,
                PageSize = 20,
                TotalCount = 0
            }
        };

        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> FlaggedInvoices(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "Flagged Invoices";
        ViewData["ActiveNav"] = "FlaggedInvoices";
        var model = await _invoices.GetFlaggedAsync(page, 10);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> InvoiceDetails(Guid id)
    {
        SetUserViewData();
        ViewData["Title"] = "Invoice Details";
        ViewData["ActiveNav"] = "FlaggedInvoices";

        var invoice = await _invoices.GetByIdAsync(id);
        if (invoice is null) return NotFound();

        var audit = await _auditLogs.GetByEntityAsync("Invoice", id, 1, 20);
        return View(new InvoiceReviewViewModel { Invoice = invoice, AuditLogs = audit });
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
    public async Task<IActionResult> IncidentActivity(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "Incident Activity";
        ViewData["ActiveNav"] = "IncidentActivity";
        var model = await _auditLogs.GetAllAsync(page, 20, "Incident");
        return View(model);
    }

   

    [HttpGet]
    public async Task<IActionResult> InvoiceReviews(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "Invoice Reviews";
        ViewData["ActiveNav"] = "InvoiceReviews";
        var model = await _auditLogs.GetAllAsync(page, 20, "Invoice");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Incidents(int page = 1, int? status = null, string? search = null)
    {
        SetUserViewData();
        ViewData["Title"] = "Incident Monitoring";
        ViewData["ActiveNav"] = "Incidents";
        ViewData["Status"] = status;
        ViewData["Search"] = search;
        var model = await _incidents.GetAllAsync(page, 10, status, search);
        return View(model);
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

    [HttpGet]
    public async Task<IActionResult> Contractors(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "Contractor Profiles";
        ViewData["ActiveNav"] = "Contractors";

        var model = await _contractorProfiles.GetAllApprovedAsync(page, 10);
        return View(model);
    }


    [HttpGet]
    public async Task<IActionResult> TenderActivity(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "Tender Activity";
        ViewData["ActiveNav"] = "TenderActivity";

        var model = await _auditLogs.GetAllAsync(page, 20, "TenderApplication");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TenderDetails(Guid id)
    {
        SetUserViewData();
        ViewData["Title"] = "Tender Details";
        ViewData["ActiveNav"] = "TenderActivity";

        if (id == Guid.Empty) return BadRequest();

        var tender = await _tenders.GetByIdAsync(id);
        if (tender is null) return NotFound();

        return View("~/Views/Admin/TenderDetails.cshtml", tender);
    }


}
