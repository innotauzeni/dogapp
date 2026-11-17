using Dog.Application;
using Dog.Infrastructure;
using Dog.Infrastructure.Middleware;
using Dog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
var builder = WebApplication.CreateBuilder(args);

// QuestPDF free Community license (valid for organisations under ~US$1M annual revenue).
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
// Bundled fonts only (Lato) — avoids missing-font blank text on servers without Arial.
QuestPDF.Settings.UseEnvironmentFonts = false;

// Background job: promote puppies to registered dogs at 6 weeks of age.
builder.Services.AddHostedService<Dog.Api.BackgroundJobs.PuppyPromotionService>();

// Add services to the container.
builder.Services.AddHttpContextAccessor();

// Wall-clock time in the business's own timezone, so timestamps the server stamps
// itself (the WhatsApp bot) line up with the ones the mobile app and portal send
// from the user's device. See AppClock for why this can't just be UtcNow.
builder.Services.AddSingleton<Dog.Api.Services.IAppClock, Dog.Api.Services.AppClock>();

// WhatsApp Bot Service — session store must be a singleton so conversation state
// survives across requests; the bot service itself stays scoped since it depends
// on scoped services (IErrorLoggingService).
builder.Services.AddSingleton<Dog.Api.Services.IWhatsAppSessionStore, Dog.Api.Services.WhatsAppSessionStore>();
builder.Services.AddScoped<Dog.Api.Services.IWhatsAppBotService, Dog.Api.Services.WhatsAppBotService>();
builder.Services.AddScoped<Dog.Application.Contracts.IWhatsAppNotificationService, Dog.Api.Services.WhatsAppNotificationService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<Dog.Api.Services.BillingDocuments>();

// Background job: daily WhatsApp digest of vaccines due, contracts expiring, and overdue invoices.
builder.Services.AddHostedService<Dog.Api.BackgroundJobs.ReminderBackgroundService>();

// Background job: alerts the configured admin phone via WhatsApp when a Critical error is logged.
builder.Services.AddHostedService<Dog.Api.BackgroundJobs.CriticalErrorAlertService>();

// Require auth on all endpoints by default
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Increase file upload size limit (default is 30MB, we need 500MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
});

// Configure Kestrel limits for large file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
});



// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Dog API build by Telthemweb", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});


// Application layer: MediatR, AutoMapper, FluentValidation, pipeline behaviors
builder.Services.AddApplicationServices();

// Infrastructure layer: DbContext, Identity, Repositories, UnitOfWork, Hangfire
builder.Services.AddPersistanceServicesDependecy(builder.Configuration);

// JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS for Angular / mobile
builder.Services.AddCorsPolicy(builder.Configuration);

// Infrastructure services: ErrorLoggingService, JwtService
builder.Services.AddInfrastructureServices();

// Cloudflare R2 file storage (dog photos, etc.)
builder.Services.AddFileStorage(builder.Configuration);


var app = builder.Build();

// ── Handle forwarded headers from Apache reverse proxy ────────────────────────
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("ApiCorsPolicy");
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Health check — no auth required, for uptime monitors / load balancers ──
app.MapGet("/health", async (ApplicationDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow })
            : Results.Json(new { status = "unhealthy", database = "unreachable", timestamp = DateTime.UtcNow }, statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "unhealthy", database = "error", message = ex.Message, timestamp = DateTime.UtcNow }, statusCode: 503);
    }
}).AllowAnonymous();

// ── Seed database on startup (Development only) ────────────────────────────
if (app.Environment.IsDevelopment())
{
    await DbSeeder.SeedAsync(app.Services);
}

app.Run();
