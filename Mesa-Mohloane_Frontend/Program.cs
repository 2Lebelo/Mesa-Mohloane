using Microsoft.AspNetCore.Authentication.Cookies;
using Mesa_Mohloane_Frontend.Services.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".MesaMohloane.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(60);
        options.SlidingExpiration = true;
    });

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException(
        "Missing ApiSettings:BaseUrl in frontend appsettings. Set it to the backend HTTPS URL, e.g. https://localhost:7242");
}

if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBaseUri))
{
    throw new InvalidOperationException(
        $"Invalid ApiSettings:BaseUrl value: '{apiBaseUrl}'. It must be an absolute URL.");
}

builder.Services.AddHttpClient("MesaApi", client =>
{
    client.BaseAddress = apiBaseUri;
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        builder.Environment.IsDevelopment()
            ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            : null
});

builder.Services.AddScoped<IAuthApiService, AuthApiService>();
builder.Services.AddScoped<IIncidentApiService, IncidentApiService>();
builder.Services.AddScoped<ITenderApiService, TenderApiService>();
builder.Services.AddScoped<IAssignmentApiService, AssignmentApiService>();
builder.Services.AddScoped<IInvoiceApiService, InvoiceApiService>();
builder.Services.AddScoped<IRatingApiService, RatingApiService>();
//builder.Services.AddScoped<RatingApiService>();
builder.Services.AddScoped<IAuditLogApiService, AuditLogApiService>();
builder.Services.AddScoped<INotificationApiService, NotificationApiService>();
builder.Services.AddScoped<IContractorProfileApiService, ContractorProfileApiService>();
builder.Services.AddScoped<IPaymentApiService, PaymentApiService>();
builder.Services.AddScoped<IUserApiService, UserApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.MapStaticAssets();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();