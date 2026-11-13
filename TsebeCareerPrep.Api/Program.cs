
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register the system time provider required by Identity services.
builder.Services.AddSingleton<TimeProvider>(
    TimeProvider.System);


// Configure a shared Data Protection key ring
// for the API and MVC applications.
var sharedKeysPath = Path.GetFullPath(
    Path.Combine(
        builder.Environment.ContentRootPath,
        "..",
        "SharedAuthKeys"));

Directory.CreateDirectory(sharedKeysPath);

builder.Services.AddDataProtection()
    .SetApplicationName("TsebeCareerPrep")
    .PersistKeysToFileSystem(
        new DirectoryInfo(sharedKeysPath));


// SQL Server database connection.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ASP.NET Core Identity configuration.
builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Development testing: lock accounts for 30 seconds.
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromSeconds(30);

        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();


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

    // API endpoints should return HTTP status codes,
    // not redirect to HTML login pages.
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


// API controllers and Swagger.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Register the interview assessment service.
builder.Services.AddScoped<
    IInterviewAssessmentService,
    InterviewAssessmentService>();

// Use Google Gemini for provisional AI assessments.
builder.Services.AddScoped<
    IAiInterviewAssessmentService,
    GeminiInterviewAssessmentService>();


builder.Services.AddHttpClient("OpenAI", client =>
{
    var baseUrl = builder.Configuration["OpenAI:BaseUrl"]
        ?? "https://api.openai.com/v1";

    var timeoutSeconds = builder.Configuration
        .GetValue<int?>("OpenAI:TimeoutSeconds") ?? 30;

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});




var app = builder.Build();

// Ensure the Administrator role exists.
// This does not assign the role to any user.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    const string administratorRole = "Administrator";

    if (!await roleManager.RoleExistsAsync(administratorRole))
    {
        var result = await roleManager.CreateAsync(
            new IdentityRole(administratorRole));

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Unable to create the Administrator role: " +
                string.Join("; ",
                    result.Errors.Select(e => e.Description)));
        }
    }
}


// Preserve existing development database seeding.
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await DatabaseSeeder.SeedAsync(context);
    }
}

// Swagger is available during development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
