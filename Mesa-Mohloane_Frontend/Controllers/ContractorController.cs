using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Frontend.Dtos;
using Mesa_Mohloane_Frontend.Services.Api;
using Mesa_Mohloane_Frontend.ViewModels;

namespace Mesa_Mohloane_Frontend.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Contractor")]
public sealed class ContractorController : BaseController
{
    private readonly IIncidentApiService _incidents;
    private readonly ITenderApiService _tenders;
    private readonly IAssignmentApiService _assignments;
    private readonly IInvoiceApiService _invoices;
    private readonly INotificationApiService _notifications;
    private readonly IContractorProfileApiService _profiles;
    private readonly IRatingApiService _ratings;

    public ContractorController(
        IHttpClientFactory http,
        IConfiguration config,
        IIncidentApiService incidents,
        ITenderApiService tenders,
        IAssignmentApiService assignments,
        IInvoiceApiService invoices,
        INotificationApiService notifications,
        IContractorProfileApiService profiles,
        IRatingApiService ratings)
        : base(http, config)
    {
        _incidents = incidents;
        _tenders = tenders;
        _assignments = assignments;
        _invoices = invoices;
        _notifications = notifications;
        _profiles = profiles;
        _ratings = ratings;

    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        SetUserViewData();
        ViewData["Title"] = "Contractor Dashboard";
        ViewData["ActiveNav"] = "Dashboard";

        var profile = await _profiles.GetMineAsync();
        ViewData["ContractorProfile"] = profile;

        var model = new ContractorDashboardViewModel
        {
            OpenIncidents = await _incidents.GetOpenAsync(1, 5),
            MyTenders = await _tenders.GetMineAsync(1, 5),
            MyAssignments = await _assignments.GetMineAsync(1, 5),
            MyInvoices = await _invoices.GetMineAsync(1, 5),
            Notifications = await _notifications.GetMineAsync(1, 5, true),
            UnreadNotifications = await _notifications.GetUnreadCountAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        SetUserViewData();
        ViewData["Title"] = "Company Profile";
        ViewData["ActiveNav"] = "Profile";

        var profile = await _profiles.GetMineAsync();
        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProfile()
    {
        SetUserViewData();
        ViewData["Title"] = "Create Company Profile";
        ViewData["ActiveNav"] = "Profile";

        var existing = await _profiles.GetMineAsync();
        if (existing is not null)
            return RedirectToAction(nameof(Profile));

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProfile(
        string companyName,
        string registrationNumber,
        string? taxNumber,
        string coverageArea)
    {
        SetUserViewData();
        ViewData["Title"] = "Create Company Profile";
        ViewData["ActiveNav"] = "Profile";

        if (string.IsNullOrWhiteSpace(companyName))
            ModelState.AddModelError(nameof(companyName), "Company name is required.");
        if (string.IsNullOrWhiteSpace(registrationNumber))
            ModelState.AddModelError(nameof(registrationNumber), "Registration number is required.");
        if (string.IsNullOrWhiteSpace(coverageArea))
            ModelState.AddModelError(nameof(coverageArea), "Coverage area is required.");

        if (!ModelState.IsValid)
            return View();

        var dto = new ContractorProfileCreateDto(
            UserId: CurrentUserId(),
            CompanyName: companyName.Trim(),
            RegistrationNumber: registrationNumber.Trim(),
            TaxNumber: string.IsNullOrWhiteSpace(taxNumber) ? null : taxNumber.Trim(),
            CoverageArea: coverageArea.Trim());

        var (ok, _, error) = await _profiles.CreateAsync(dto);
        if (!ok)
        {
            ViewData["Error"] = error ?? "Failed to create company profile.";
            return View();
        }

        TempData["Success"] = "Company profile created successfully. It must be approved before tender submission.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        SetUserViewData();
        ViewData["Title"] = "Edit Company Profile";
        ViewData["ActiveNav"] = "Profile";

        var profile = await _profiles.GetMineAsync();
        if (profile is null)
            return RedirectToAction(nameof(CreateProfile));

        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(
        Guid id,
        string companyName,
        string registrationNumber,
        string? taxNumber,
        string coverageArea)
    {
        SetUserViewData();
        ViewData["Title"] = "Edit Company Profile";
        ViewData["ActiveNav"] = "Profile";

        if (id == Guid.Empty)
            ModelState.AddModelError(nameof(id), "Invalid profile id.");
        if (string.IsNullOrWhiteSpace(companyName))
            ModelState.AddModelError(nameof(companyName), "Company name is required.");
        if (string.IsNullOrWhiteSpace(registrationNumber))
            ModelState.AddModelError(nameof(registrationNumber), "Registration number is required.");
        if (string.IsNullOrWhiteSpace(coverageArea))
            ModelState.AddModelError(nameof(coverageArea), "Coverage area is required.");

        if (!ModelState.IsValid)
        {
            var profile = await _profiles.GetMineAsync();
            return View(profile);
        }

        var dto = new ContractorProfileUpdateDto(
            CompanyName: companyName.Trim(),
            RegistrationNumber: registrationNumber.Trim(),
            TaxNumber: string.IsNullOrWhiteSpace(taxNumber) ? null : taxNumber.Trim(),
            CoverageArea: coverageArea.Trim());

        var (ok, _, error) = await _profiles.UpdateAsync(id, dto);
        if (!ok)
        {
            ViewData["Error"] = error ?? "Failed to update company profile.";
            var profile = await _profiles.GetMineAsync();
            return View(profile);
        }

        TempData["Success"] = "Company profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> OpenIncidents(int page = 1, string? search = null)
    {
        SetUserViewData();
        ViewData["Title"] = "Open Incidents";
        ViewData["ActiveNav"] = "OpenIncidents";
        ViewData["Search"] = search;
        var model = await _incidents.GetOpenAsync(page, 10, search);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> IncidentDetails(Guid id)
    {
        SetUserViewData();
        ViewData["Title"] = "Incident Details";
        ViewData["ActiveNav"] = "OpenIncidents";

        var incident = await _incidents.GetByIdAsync(id);
        if (incident is null) return NotFound();
        return View(incident);
    }


    [HttpGet]
    public async Task<IActionResult> SubmitTender(Guid incidentId)
    {
        SetUserViewData();
        ViewData["Title"] = "Submit Tender";
        ViewData["ActiveNav"] = "OpenIncidents";

        if (incidentId == Guid.Empty)
            return BadRequest();

        var incident = await _incidents.GetByIdAsync(incidentId);
        if (incident is null) return NotFound();

        return View(new SubmitTenderViewModel
        {
            IncidentId = incidentId,
            Incident = incident
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitTender(TenderApplicationCreateDto dto)
    {
        var contractorId = CurrentUserId();
        if (contractorId == Guid.Empty)
        {
            TempData["Error"] = "Your session is missing a valid contractor id. Please sign in again.";
            return RedirectToAction(nameof(OpenIncidents));
        }

        dto = dto with
        {
            ContractorId = contractorId,
            QuotedTotalAmount = dto.LineItems?.Sum(x => x.LineTotal) ?? dto.QuotedTotalAmount
        };

        if (dto.IncidentId == Guid.Empty ||
            string.IsNullOrWhiteSpace(dto.ProposalText) ||
            dto.EstimatedTimelineDays <= 0 ||
            dto.LineItems is null ||
            dto.LineItems.Count == 0 ||
            dto.QuotedTotalAmount <= 0)
        {
            TempData["Error"] = "Complete the proposal, timeline, and at least one quotation line item.";
            return RedirectToAction(nameof(SubmitTender), new { incidentId = dto.IncidentId });
        }

        var lineTotal = dto.LineItems.Sum(x => x.LineTotal);
        if (lineTotal != dto.QuotedTotalAmount)
        {
            TempData["Error"] = "Quotation line items must sum exactly to the quoted total.";
            return RedirectToAction(nameof(SubmitTender), new { incidentId = dto.IncidentId });
        }

        var (ok, data, error) = await _tenders.SubmitTenderAsync(dto);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Tender submitted successfully."
            : error ?? "Failed to submit tender.";

        return ok && data is not null
            ? RedirectToAction(nameof(TenderDetails), new { id = data.Id })
            : RedirectToAction(nameof(SubmitTender), new { incidentId = dto.IncidentId });
    }

    [HttpGet]
    public async Task<IActionResult> MyTenders(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "My Tenders";
        ViewData["ActiveNav"] = "MyTenders";

        var model = await _tenders.GetMineAsync(page, 10);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TenderDetails(Guid id)
    {
        SetUserViewData();
        ViewData["Title"] = "Tender Details";
        ViewData["ActiveNav"] = "MyTenders";

        if (id == Guid.Empty) return BadRequest();

        var tender = await _tenders.GetByIdAsync(id);
        if (tender is null) return NotFound();

        return View(tender);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WithdrawTender(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "Invalid tender id.";
            return RedirectToAction(nameof(MyTenders));
        }

        var (ok, error) = await _tenders.WithdrawAsync(id);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Tender withdrawn successfully."
            : error ?? "Failed to withdraw tender.";

        return RedirectToAction(nameof(MyTenders));
    }


    [HttpGet]
    public async Task<IActionResult> MyAssignments(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "My Assignments";
        ViewData["ActiveNav"] = "MyAssignments";
        var model = await _assignments.GetMineAsync(page, 10);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignmentDetails(Guid id)
    {
        SetUserViewData();
        ViewData["Title"] = "Assignment Details";
        ViewData["ActiveNav"] = "MyAssignments";
        var model = await _assignments.GetByIdAsync(id);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkInProgress(Guid id)
    {
        var (ok, _, error) = await _assignments.StartAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "Job marked as in progress." : error ?? "Failed to start job.";
        return RedirectToAction(nameof(MyAssignments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCompleted(
    Guid id,
    string completionSummary,
    IFormFile? completionEvidenceFile)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "Invalid assignment id.";
            return RedirectToAction(nameof(MyAssignments));
        }

        if (string.IsNullOrWhiteSpace(completionSummary))
        {
            TempData["Error"] = "Completion summary is required.";
            return RedirectToAction(nameof(AssignmentDetails), new { id });
        }

        if (completionEvidenceFile is null || completionEvidenceFile.Length == 0)
        {
            TempData["Error"] = "Completion evidence photo is required.";
            return RedirectToAction(nameof(AssignmentDetails), new { id });
        }

        var (ok, _, error) = await _assignments.CompleteWithEvidenceAsync(
            id,
            completionSummary,
            completionEvidenceFile);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Completion report submitted successfully."
            : error ?? "Failed to submit completion report.";

        return RedirectToAction(nameof(AssignmentDetails), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> SubmitInvoice(Guid assignmentId)
    {
        SetUserViewData();

        ViewData["Title"] = "Submit Invoice";
        ViewData["ActiveNav"] = "Invoices";

        if (assignmentId == Guid.Empty)
        {
            TempData["Error"] = "Invalid assignment id.";
            return RedirectToAction(nameof(MyAssignments));
        }

        var assignment = await _assignments.GetByIdAsync(assignmentId);

        if (assignment is null)
            return NotFound();

        if (assignment.Status != 5)
        {
            TempData["Error"] = "You can only submit an invoice after the assignment has been approved.";
            return RedirectToAction(nameof(AssignmentDetails), new { id = assignmentId });
        }

        var existingInvoice = await _invoices.GetByAssignmentAsync(assignmentId);

        //TenderApplicationDto? tender = null;
        //decimal originalQuotedAmount = 0m;

        //tender = await _tenders.GetByIdAsync(assignment.TenderApplicationId);

        //if (tender is not null)
        //    originalQuotedAmount = tender.QuotedTotalAmount;
        var tender = await _tenders.GetByIdAsync(assignment.TenderApplicationId);

        if (tender is null)
        {
            TempData["Error"] = "Approved tender details could not be loaded. Please try again.";
            return RedirectToAction(nameof(AssignmentDetails), new { id = assignmentId });
        }

        var originalQuotedAmount = tender.QuotedTotalAmount;

        return View(new SubmitInvoiceViewModel
        {
            AssignmentId = assignmentId,
            Assignment = assignment,
            Tender = tender,
            OriginalQuotedAmount = originalQuotedAmount,
            ExistingInvoice = existingInvoice
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitInvoice(SubmitInvoiceFormViewModel form)
    {
        if (form.AssignmentId == Guid.Empty)
        {
            TempData["Error"] = "Invalid assignment id.";
            return RedirectToAction(nameof(MyAssignments));
        }

        var contractorId = CurrentUserId();
        if (contractorId == Guid.Empty)
        {
            TempData["Error"] = "Your session is missing a valid contractor id. Please sign in again.";
            return RedirectToAction(nameof(MyAssignments));
        }

        if (form.TenderApplicationId == Guid.Empty)
        {
            TempData["Error"] = "Invalid tender reference.";
            return RedirectToAction(nameof(SubmitInvoice), new { assignmentId = form.AssignmentId });
        }

        var assignment = await _assignments.GetByIdAsync(form.AssignmentId);
        if (assignment is null)
        {
            TempData["Error"] = "Assignment could not be found.";
            return RedirectToAction(nameof(MyAssignments));
        }

        if (assignment.ContractorId != contractorId)
        {
            TempData["Error"] = "You are not authorised to submit an invoice for this assignment.";
            return RedirectToAction(nameof(MyAssignments));
        }

        if (assignment.Status != 5)
        {
            TempData["Error"] = "You can only submit an invoice after the assignment has been approved.";
            return RedirectToAction(nameof(AssignmentDetails), new { id = form.AssignmentId });
        }

        if (assignment.TenderApplicationId != form.TenderApplicationId)
        {
            TempData["Error"] = "Invoice tender reference does not match this assignment.";
            return RedirectToAction(nameof(SubmitInvoice), new { assignmentId = form.AssignmentId });
        }

        var tender = await _tenders.GetByIdAsync(form.TenderApplicationId);
        if (tender is null)
        {
            TempData["Error"] = "Approved tender reference could not be found.";
            return RedirectToAction(nameof(AssignmentDetails), new { id = form.AssignmentId });
        }

        if (tender.ContractorId != contractorId)
        {
            TempData["Error"] = "The approved tender does not belong to your contractor account.";
            return RedirectToAction(nameof(MyAssignments));
        }

        if (form.LineItems is null || form.LineItems.Count == 0)
        {
            TempData["Error"] = "Add at least one invoice line item.";
            return RedirectToAction(nameof(SubmitInvoice), new { assignmentId = form.AssignmentId });
        }

        var validLineItems = form.LineItems
            .Where(x =>
                x.Category is >= 1 and <= 5 &&
                !string.IsNullOrWhiteSpace(x.Description) &&
                !string.IsNullOrWhiteSpace(x.UnitOfMeasure) &&
                x.Quantity > 0 &&
                x.UnitPrice >= 0)
            .ToList();

        if (validLineItems.Count == 0)
        {
            TempData["Error"] = "Add at least one valid invoice line item.";
            return RedirectToAction(nameof(SubmitInvoice), new { assignmentId = form.AssignmentId });
        }

        foreach (var item in validLineItems)
        {
            item.LineTotal = Math.Round(item.Quantity * item.UnitPrice, 2);
        }

        var calculatedTotal = validLineItems.Sum(x => x.LineTotal);

        if (calculatedTotal <= 0)
        {
            TempData["Error"] = "Invoice total must be greater than zero.";
            return RedirectToAction(nameof(SubmitInvoice), new { assignmentId = form.AssignmentId });
        }

        var invoiceNumber = string.IsNullOrWhiteSpace(form.InvoiceNumber)
            ? $"INV-WEB-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : form.InvoiceNumber.Trim();

        var dto = new InvoiceCreateDto(
            AssignmentId: form.AssignmentId,
            TenderApplicationId: form.TenderApplicationId,
            ContractorId: contractorId,
            InvoiceNumber: invoiceNumber,
            OriginalQuotedAmount: tender.QuotedTotalAmount,
            FinalInvoiceAmount: calculatedTotal,
            LineItems: validLineItems.Select(x => x.ToDto()).ToList());

        var (ok, data, error) = await _invoices.SubmitAsync(dto);

        TempData[ok ? "Success" : "Error"] = ok
            ? $"Invoice {data?.InvoiceNumber ?? invoiceNumber} submitted successfully."
            : error ?? "Failed to submit invoice.";

        return ok
            ? RedirectToAction(nameof(MyInvoices))
            : RedirectToAction(nameof(SubmitInvoice), new { assignmentId = form.AssignmentId });
    }

    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> SubmitInvoice(InvoiceCreateDto dto)
    //{
    //    if (dto.AssignmentId == Guid.Empty)
    //    {
    //        TempData["Error"] = "Invalid assignment id.";
    //        return RedirectToAction(nameof(MyAssignments));
    //    }

    //    if (dto.TenderApplicationId == Guid.Empty)
    //    {
    //        TempData["Error"] = "Invalid tender reference.";
    //        return RedirectToAction(nameof(SubmitInvoice), new { assignmentId = dto.AssignmentId });
    //    }

    //    if (dto.LineItems is null || dto.LineItems.Count == 0)
    //    {
    //        TempData["Error"] = "Add at least one invoice line item.";
    //        return RedirectToAction(nameof(SubmitInvoice), new { assignmentId = dto.AssignmentId });
    //    }

    //    var calculatedTotal = dto.LineItems.Sum(x => x.LineTotal);

    //    dto = dto with
    //    {
    //        FinalInvoiceAmount = calculatedTotal
    //    };

    //    var (ok, _, error) = await _invoices.SubmitAsync(dto);

    //    TempData[ok ? "Success" : "Error"] = ok
    //        ? "Invoice submitted successfully."
    //        : error ?? "Failed to submit invoice.";

    //    return ok
    //        ? RedirectToAction(nameof(MyInvoices))
    //        : RedirectToAction(nameof(SubmitInvoice), new { assignmentId = dto.AssignmentId });
    //}

    [HttpGet]
    public async Task<IActionResult> MyInvoices(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "My Invoices";
        ViewData["ActiveNav"] = "Invoices";
        var model = await _invoices.GetMineAsync(page, 10);
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
    public async Task<IActionResult> Ratings(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "Ratings";
        ViewData["ActiveNav"] = "Ratings";

        var contractorId = CurrentUserId();

        if (contractorId == Guid.Empty)
        {
            TempData["Error"] = "Your session is missing a valid contractor id. Please sign in again.";
            return RedirectToAction(nameof(Dashboard));
        }

        var ratings = await _ratings.GetByContractorAsync(contractorId, page, 10);

        return View(new ContractorRatingsViewModel
        {
            Ratings = ratings
        });
    }

    private Guid CurrentUserId()
    {
        var raw = HttpContext.Session.GetString("user_id")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
