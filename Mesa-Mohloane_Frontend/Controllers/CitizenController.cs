using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Frontend.Dtos;
using Mesa_Mohloane_Frontend.Services.Api;
using Mesa_Mohloane_Frontend.ViewModels;

namespace Mesa_Mohloane_Frontend.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Citizen")]
public sealed class CitizenController : BaseController
{
    private readonly IIncidentApiService _incidents;
    private readonly IAssignmentApiService _assignments;
    private readonly IRatingApiService _ratings;
    private readonly INotificationApiService _notifications;

    public CitizenController(
        IHttpClientFactory http,
        IConfiguration config,
        IIncidentApiService incidents,
        IAssignmentApiService assignments,
        IRatingApiService ratings,
        INotificationApiService notifications)
        : base(http, config)
    {
        _incidents = incidents;
        _assignments = assignments;
        _ratings = ratings;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        SetUserViewData();
        ViewData["Title"] = "Citizen Dashboard";
        ViewData["ActiveNav"] = "Dashboard";

        var incidents = await _incidents.GetMineAsync(1, 6);
        var model = new CitizenDashboardViewModel
        {
            MyIncidents = incidents,
            CompletedJobsCount = incidents?.Items.Count(i => i.Status is 6 or 7) ?? 0,
            Notifications = await _notifications.GetMineAsync(1, 5, true),
            UnreadNotifications = await _notifications.GetUnreadCountAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult ReportIncident()
    {
        SetUserViewData();
        ViewData["Title"] = "Report Incident";
        ViewData["ActiveNav"] = "ReportIncident";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> ReportIncident(string title, string description, string locationName, decimal latitude, decimal longitude, List<IFormFile>? photos)
    {
        SetUserViewData();
        ViewData["Title"] = "Report Incident";
        ViewData["ActiveNav"] = "ReportIncident";

        ValidateIncidentInput(title, description, locationName, latitude, longitude, photos);
        if (!ModelState.IsValid) return View();

        var (ok, data, error) = await _incidents.CreateAsync(title, description, locationName, latitude, longitude, ToFormFileCollection(photos));
        if (!ok || data is null)
        {
            TempData["Error"] = error ?? "Failed to report incident.";
            return View();
        }

        TempData["Success"] = $"Incident {data.IncidentNumber} reported successfully.";
        return RedirectToAction(nameof(IncidentDetails), new { id = data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> MyIncidents(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "My Incidents";
        ViewData["ActiveNav"] = "MyIncidents";
        return View(await _incidents.GetMineAsync(page, 10));
    }

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> IncidentDetails(Guid id)
    {
        SetUserViewData();

        ViewData["Title"] = "Incident Details";
        ViewData["ActiveNav"] = "MyIncidents";

        if (id == Guid.Empty)
            return BadRequest();

        var incident = await _incidents.GetByIdAsync(id);

        if (incident is null)
            return NotFound();

        AssignmentDto? assignment = null;

        if (incident.Status >= 4)
        {
            assignment = await _assignments.GetByIncidentAsync(incident.Id);
        }

        var model = new CitizenIncidentDetailViewModel
        {
            Incident = incident,
            Assignment = assignment
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditIncident(Guid id)
    {
        SetUserViewData();
        ViewData["Title"] = "Edit Incident";
        ViewData["ActiveNav"] = "MyIncidents";
        var incident = await _incidents.GetByIdAsync(id);
        if (incident is null) return NotFound();
        if (incident.Status != 0)
        {
            TempData["Error"] = "Only pending incidents can be edited.";
            return RedirectToAction(nameof(IncidentDetails), new { id });
        }
        return View(incident);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditIncident(Guid id, string title, string description, string locationName, decimal latitude, decimal longitude)
    {
        ValidateIncidentInput(title, description, locationName, latitude, longitude, null);
        if (!ModelState.IsValid)
        {
            SetUserViewData();
            ViewData["Title"] = "Edit Incident";
            ViewData["ActiveNav"] = "MyIncidents";
            var existing = await _incidents.GetByIdAsync(id);
            return existing is null ? NotFound() : View(existing);
        }

        var dto = new IncidentUpdateRequestDto(title.Trim(), description.Trim(), locationName.Trim(), latitude, longitude, 0);
        var (ok, _, error) = await _incidents.UpdateAsync(id, dto);
        TempData[ok ? "Success" : "Error"] = ok ? "Incident updated successfully." : error ?? "Failed to update incident.";
        return RedirectToAction(nameof(IncidentDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPhotos(Guid id, List<IFormFile>? photos)
    {
        var files = ToFormFileCollection(photos);
        if (files.Count == 0)
        {
            TempData["Error"] = "Please choose at least one photo.";
            return RedirectToAction(nameof(IncidentDetails), new { id });
        }
        var (ok, _, error) = await _incidents.AddPhotosAsync(id, files);
        TempData[ok ? "Success" : "Error"] = ok ? "Photo(s) added successfully." : error ?? "Failed to add photos.";
        return RedirectToAction(nameof(IncidentDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(Guid incidentId, Guid photoId)
    {
        var (ok, error) = await _incidents.DeletePhotoAsync(incidentId, photoId);
        TempData[ok ? "Success" : "Error"] = ok ? "Photo removed successfully." : error ?? "Failed to remove photo.";
        return RedirectToAction(nameof(IncidentDetails), new { id = incidentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteIncident(Guid id)
    {
        var (ok, error) = await _incidents.DeleteAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "Incident deleted successfully." : error ?? "Failed to delete incident.";
        return RedirectToAction(nameof(MyIncidents));
    }

    [HttpGet]
    public async Task<IActionResult> CompletedJobs(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "Completed Jobs";
        ViewData["ActiveNav"] = "CompletedJobs";
        var incidents = await _incidents.GetMineAsync(page, 10);
        var completed = incidents?.Items.Where(i => i.Status is 6 or 7).ToList() ?? new List<IncidentListItemDto>();
        var items = new List<CompletedJobItem>();
        foreach (var incident in completed)
            items.Add(new CompletedJobItem { Incident = incident, Assignment = await _assignments.GetByIncidentAsync(incident.Id), Rating = null });
        return View(new CompletedJobsViewModel { Items = items });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeAssignment(Guid assignmentId, Guid incidentId)
    {
        if (assignmentId == Guid.Empty)
        {
            TempData["Error"] = "Invalid assignment id.";
            return RedirectToAction(nameof(IncidentDetails), new { id = incidentId });
        }

        if (incidentId == Guid.Empty)
        {
            TempData["Error"] = "Invalid incident id.";
            return RedirectToAction(nameof(MyIncidents));
        }

        var (ok, _, error) = await _assignments.AcknowledgeAsync(assignmentId);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Work completion acknowledged successfully. The administrator can now approve the completion."
            : error ?? "Failed to acknowledge work completion.";

        return RedirectToAction(nameof(IncidentDetails), new { id = incidentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RateContractor(ContractorRatingCreateDto dto)
    {
        if (dto.Stars is < 1 or > 5)
        {
            TempData["Error"] = "Rating must be between 1 and 5 stars.";
            return RedirectToAction(nameof(CompletedJobs));
        }
        var (ok, _, error) = await _ratings.RateAsync(dto);
        TempData[ok ? "Success" : "Error"] = ok ? "Contractor rating submitted successfully." : error ?? "Failed to submit contractor rating.";
        return RedirectToAction(nameof(CompletedJobs));
    }

    [HttpGet]
    public async Task<IActionResult> Notifications(int page = 1)
    {
        SetUserViewData();
        ViewData["Title"] = "Notifications";
        ViewData["ActiveNav"] = "Notifications";
        return View(await _notifications.GetMineAsync(page, 20, false));
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

    private static FormFileCollection ToFormFileCollection(List<IFormFile>? photos)
    {
        var files = new FormFileCollection();
        if (photos is null) return files;
        foreach (var photo in photos.Where(p => p.Length > 0)) files.Add(photo);
        return files;
    }

    private void ValidateIncidentInput(string title, string description, string locationName, decimal latitude, decimal longitude, List<IFormFile>? photos)
    {
        if (string.IsNullOrWhiteSpace(title)) ModelState.AddModelError(nameof(title), "Title is required.");
        if (title?.Length > 120) ModelState.AddModelError(nameof(title), "Title cannot exceed 120 characters.");
        if (string.IsNullOrWhiteSpace(description)) ModelState.AddModelError(nameof(description), "Description is required.");
        if (description?.Length < 20) ModelState.AddModelError(nameof(description), "Description must be at least 20 characters.");
        if (string.IsNullOrWhiteSpace(locationName)) ModelState.AddModelError(nameof(locationName), "Location name is required.");
        if (latitude < -90 || latitude > 90) ModelState.AddModelError(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude < -180 || longitude > 180) ModelState.AddModelError(nameof(longitude), "Longitude must be between -180 and 180.");
        if (photos is null) return;
        foreach (var photo in photos.Where(p => p.Length > 0))
        {
            if (!photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) ModelState.AddModelError(nameof(photos), $"{photo.FileName} is not a valid image.");
            if (photo.Length > 5_000_000) ModelState.AddModelError(nameof(photos), $"{photo.FileName} exceeds 5MB.");
        }
    }
}
