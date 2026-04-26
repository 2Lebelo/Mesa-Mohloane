using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services;
using Mesa_Mohloane_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/mesamohloane-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Database 
builder.Services.AddDbContext<MesaMohloaneDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication 
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// CORS 
builder.Services.AddCors(opt =>
    opt.AddPolicy("Mesa-Mohloane_Frontend", policy =>
        policy.WithOrigins("https://localhost:7001")
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Repositories 
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();

builder.Services.AddHttpContextAccessor();

// Services 
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// Controllers 
builder.Services.AddControllers();

// ═════════════════════════════════════════════════════════════════════════════
var app = builder.Build();
// ═════════════════════════════════════════════════════════════════════════════

//  Database migration + seeding (runs automatically on every startup) 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MesaMohloaneDbContext>();

    // 1. Apply any pending EF migrations — safe to call even when up to date
    await db.Database.MigrateAsync();

    // 2. Seed roles 
    // Uses AnyAsync (not FirstOrDefault) — lightweight existence check only.
    // Add new roles here in future; existing rows are never touched.
    var rolesToSeed = new[]
    {
        new { Name = "Administrator", Description = "Full system access — manages users, assignments and approvals" },
        new { Name = "Contractor",    Description = "Applies for tenders, completes jobs and submits invoices" },
        new { Name = "Citizen",       Description = "Reports infrastructure incidents and rates completed work" },
        new { Name = "Inspector",     Description = "Audits system activity and monitors fairness and transparency" }
    };

    foreach (var r in rolesToSeed)
    {
        if (!await db.Roles.AnyAsync(x => x.Name == r.Name))
        {
            db.Roles.Add(new Role
            {
                Name = r.Name,
                Description = r.Description
            });
        }
    }

    await db.SaveChangesAsync();

    // 3. Seed the administrator account 
    // Credentials come from appsettings / environment variables — never hardcoded.
    var adminEmail = builder.Configuration["AdminSeed:Email"]?.Trim();
    var adminPassword = builder.Configuration["AdminSeed:Password"];

    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        // Role is guaranteed to exist — we just seeded it above
        var adminRole = await db.Roles.FirstAsync(r => r.Name == "Administrator");

        var adminUser = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (adminUser is null)
        {
            db.Users.Add(new User
            {
                FirstName = "Mesa",
                LastName = "Mohloane",
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                IsActive = true,
                RoleId = adminRole.Id,
                IsDeleted = false
            });
        }
        else
        {
            // Re-sync on every startup — useful if password is rotated via config
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            adminUser.IsActive = true;
            adminUser.IsDeleted = false;
            adminUser.RoleId = adminRole.Id;
            adminUser.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }
}

// Middleware pipeline 
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("Mesa-Mohloane_Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();