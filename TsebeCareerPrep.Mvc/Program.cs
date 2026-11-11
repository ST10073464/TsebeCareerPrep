
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1. SHARED DATA PROTECTION
// ======================================================

// Use the same Data Protection key ring as the API.
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


// ======================================================
// 2. MVC CONTROLLERS AND RAZOR VIEWS
// ======================================================

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();


// ======================================================
// 3. INTERVIEW CATEGORY API SERVICE
// ======================================================

builder.Services.AddHttpClient<
    TsebeCareerPrep.Mvc.Services.InterviewCategoryService>(
    client =>
    {
        var apiBaseUrl =
            builder.Configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "API BaseUrl is not configured.");

        client.BaseAddress = new Uri(apiBaseUrl);
    });


// ======================================================
// 4. AUTHENTICATION API SERVICE
// ======================================================

builder.Services.AddHttpClient<
    TsebeCareerPrep.Mvc.Services.AuthenticationService>(
    client =>
    {
        var apiBaseUrl =
            builder.Configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "API BaseUrl is not configured.");

        client.BaseAddress = new Uri(apiBaseUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            // Do not store or automatically forward cookies.
            // This prevents cookies from being shared
            // between users through pooled HTTP handlers.
            UseCookies = false
        });


// ======================================================
// 5. MVC COOKIE AUTHENTICATION
// ======================================================

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(
        IdentityConstants.ApplicationScheme,
        options =>
        {
            options.Cookie.Name = "TsebeCareerPrep.Auth";

            options.Cookie.HttpOnly = true;

            options.Cookie.SecurePolicy =
                CookieSecurePolicy.Always;

            options.Cookie.SameSite = SameSiteMode.Lax;

            options.LoginPath = "/Account/Login";

            options.AccessDeniedPath =
                "/Account/AccessDenied";

            options.ExpireTimeSpan =
                TimeSpan.FromMinutes(30);

            options.SlidingExpiration = true;
        });


// ======================================================
// AUTHENTICATED INTERVIEW API HTTP CLIENT
// ======================================================

builder.Services.AddTransient<
    TsebeCareerPrep.Mvc.Services.ApiAuthenticationHandler>();

builder.Services.AddHttpClient(
    "AuthenticatedInterviewApi",
    client =>
    {
        var apiBaseUrl =
            builder.Configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "API BaseUrl is not configured.");

        client.BaseAddress = new Uri(apiBaseUrl);

        // Prevent credentials being forwarded through redirects.
        client.DefaultRequestVersion =
            HttpVersion.Version11;
    })
    .AddHttpMessageHandler<
        TsebeCareerPrep.Mvc.Services.ApiAuthenticationHandler>()
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            UseCookies = false,
            AllowAutoRedirect = false
        });



// ======================================================
// 6. AUTHORIZATION
// ======================================================

builder.Services.AddAuthorization();


// ======================================================
// 7. BUILD APPLICATION
// ======================================================

var app = builder.Build();


// ======================================================
// 8. HTTP REQUEST PIPELINE
// ======================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Authentication must run before authorization.
app.UseAuthentication();

app.UseAuthorization();


// ======================================================
// 9. MVC ROUTING
// ======================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
