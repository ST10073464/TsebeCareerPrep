
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1. SYSTEM TIME PROVIDER
// ======================================================

builder.Services.AddSingleton<TimeProvider>(
    TimeProvider.System);


// ======================================================
// 2. DATA PROTECTION
// ======================================================

// Read the configured Data Protection directory.
// Azure environment variable:
// DataProtection__KeysPath=/tmp/TsebeCareerPrepKeys

var configuredKeysPath =
    builder.Configuration["DataProtection:KeysPath"];

// Use a writable temporary directory when no path
// has been configured.

var sharedKeysPath =
    string.IsNullOrWhiteSpace(configuredKeysPath)
        ? Path.Combine(
            Path.GetTempPath(),
            "TsebeCareerPrepKeys")
        : configuredKeysPath;

// Ensure the directory exists.

Directory.CreateDirectory(sharedKeysPath);

// Configure Data Protection.

builder.Services.AddDataProtection()
    .SetApplicationName("TsebeCareerPrep")
    .PersistKeysToFileSystem(
        new DirectoryInfo(sharedKeysPath));


// ======================================================
// 3. SQL SERVER DATABASE
// ======================================================

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection"));
    });


// ======================================================
// 4. ASP.NET CORE IDENTITY
// ======================================================

builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromSeconds(30);

        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();


// ======================================================
// 5. AUTHENTICATION
// ======================================================

builder.Services
    .AddAuthentication(
        IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.AddAuthorization();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "TsebeCareerPrep.Auth";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite =
        SameSiteMode.Lax;

    options.ExpireTimeSpan =
        TimeSpan.FromMinutes(30);

    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;

        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode =
            StatusCodes.Status403Forbidden;

        return Task.CompletedTask;
    };
});


// ======================================================
// 6. API CONTROLLERS AND SWAGGER
// ======================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();


// ======================================================
// 7. INTERVIEW ASSESSMENT SERVICE
// ======================================================

builder.Services.AddScoped<
    IInterviewAssessmentService,
    InterviewAssessmentService>();


// ======================================================
// 8. OPENAI HTTP CLIENT
// ======================================================

builder.Services.AddHttpClient("OpenAI", client =>
{
    var baseUrl =
        builder.Configuration["OpenAI:BaseUrl"]
        ?? "https://api.openai.com/v1";

    var timeoutSeconds =
        builder.Configuration.GetValue<int?>(
            "OpenAI:TimeoutSeconds") ?? 30;

    client.BaseAddress =
        new Uri(baseUrl.TrimEnd('/') + "/");

    client.Timeout =
        TimeSpan.FromSeconds(timeoutSeconds);
});


// ======================================================
// 9. BUILD APPLICATION
// ======================================================

var app = builder.Build();


// ======================================================
// 10. ENSURE REQUIRED IDENTITY ROLES EXIST
// ======================================================

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    // All supported application roles.
    // Public registration assigns Candidate only.
    // SuperAdministrator must be granted separately
    // through a trusted administrative process.

    string[] requiredRoles =
    {
        "SuperAdministrator",
        "Administrator",
        "Candidate"
    };

    foreach (var roleName in requiredRoles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create the {roleName} role: " +
                    string.Join("; ",
                        result.Errors.Select(
                            error => error.Description)));
            }
        }
    }
}


// ======================================================
// 11. DEVELOPMENT DATABASE SEEDING
// ======================================================

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        await DatabaseSeeder.SeedAsync(context);
    }
}


// ======================================================
// 12. SWAGGER
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ======================================================
// 13. HTTP REQUEST PIPELINE
// ======================================================

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();


// ======================================================
// 14. API ENDPOINTS
// ======================================================

app.MapControllers();

app.Run();
