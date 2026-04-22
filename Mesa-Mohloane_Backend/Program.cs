using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.Entities;
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

// CORS — allow client origin
builder.Services.AddCors(opt =>
    opt.AddPolicy("LibraClient", policy =>
        policy.WithOrigins("https://localhost:7001")  // client URL
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Services
builder.Services.AddScoped<JwtHelper>();

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MesaMohloaneDbContext>();
    await db.Database.MigrateAsync();

    var adminEmail = builder.Configuration["AdminSeed:Email"]?.Trim();
    var adminPassword = builder.Configuration["AdminSeed:Password"];

    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole is null)
        {
            adminRole = new Role
            {
                Name = "Admin",
                Description = "Full system access"
            };

            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();
        }

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
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            adminUser.IsActive = true;
            adminUser.IsDeleted = false;
            adminUser.RoleId = adminRole.Id;
            adminUser.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("LibraClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();