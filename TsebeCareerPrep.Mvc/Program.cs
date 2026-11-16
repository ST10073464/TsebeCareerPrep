
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1. SHARED DATA PROTECTION
// ======================================================

// Read the configured key directory.
// Azure uses DataProtection__KeysPath.
// If no path is configured, use a writable temporary
// directory instead of a restricted root directory.

var configuredKeysPath =
    builder.Configuration["DataProtection:KeysPath"];

var sharedKeysPath = string.IsNullOrWhiteSpace(configuredKeysPath)
    ? Path.Combine(
        Path.GetTempPath(),
        "TsebeCareerPrepKeys")
    : configuredKeysPath;

// Create the directory before configuring Data Protection.
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
            // Prevent cookies from being shared between users.
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
// 6. AUTHENTICATED INTERVIEW API HTTP CLIENT
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
// 7. AUTHORIZATION
// ======================================================

builder.Services.AddAuthorization();


// ======================================================
// 8. BUILD APPLICATION
// ======================================================

var app = builder.Build();


// ======================================================
// 9. HTTP REQUEST PIPELINE
// ======================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();


// ======================================================
// 10. MVC ROUTING
// ======================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
