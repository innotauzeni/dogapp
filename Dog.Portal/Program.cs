using Dog.Application;
using Dog.Application.Authorization;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF free Community license (valid for organisations under ~US$1M annual revenue).
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
// Use only QuestPDF's bundled fonts (Lato) so PDFs render the same on any server — system fonts
// such as Arial may be missing on the host, which made certificate text disappear.
QuestPDF.Settings.UseEnvironmentFonts = false;
var latoFonts = Dog.Application.Common.Pdf.PdfFonts.Register();
Console.WriteLine(latoFonts > 0 ? $"PDF fonts: registered {latoFonts} Lato files." : "PDF fonts: WARNING - no Lato-*.ttf found next to the application; PDFs will print without text. Copy the Lato-*.ttf files from the publish folder.");

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

// Increase file upload size limit (default is 30MB, we need 500MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
});

// Register custom services
builder.Services.AddScoped<ICurrentUserInfoService, CurrentUserInfoService>();

// Cloudflare R2 file storage (dog photos)
builder.Services.AddFileStorage(builder.Configuration);

// Permission policy provider — reads "permission" claims from the cookie principal.
// Only register the policy infrastructure, NOT the full Application services
// (which would pull in MediatR handlers that need Infrastructure DI registrations).
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
//builder.Services.AddScoped<ICurrentUserInfoService, CurrentUserInfoService>();


// Cookie-based auth for the Portal UI
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });


// Session — stores JWT + menus from the API after login
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// HttpClient that calls the API — all data goes through here
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]!;
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    // Bypass dev SSL cert validation
    client.Timeout = TimeSpan.FromSeconds(120);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});


var app = builder.Build();

// ── Handle forwarded headers from Apache reverse proxy ────────────────────────
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "landing",
    pattern: "",
    defaults: new { controller = "Landing", action = "Index" });

app.MapControllerRoute(
    name: "controller-only",
    pattern: "{controller}",
    defaults: new { action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}/{id?}",
    defaults: new { action = "Index" });

app.Run();
