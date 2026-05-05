using Dog.Application;
using Dog.Infrastructure;
using Dog.Infrastructure.Middleware;
using Dog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();

// Require auth on all endpoints by default
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
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


var app = builder.Build();

// ── Handle forwarded headers from Apache reverse proxy ────────────────────────
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ApiCorsPolicy");
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Seed database on startup ──────────────────────────────────────────────
await DbSeeder.SeedAsync(app.Services);


app.Run();
