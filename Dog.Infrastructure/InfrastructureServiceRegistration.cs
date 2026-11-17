using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Services;
using Dog.Core.Entities;
using Dog.Infrastructure.Persistence;
using Dog.Infrastructure.Repositories;
using Dog.Infrastructure.Services;
using Hangfire;
using Hangfire.MySql;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using static Dog.Application.Services.IErrorLoggingService;

namespace Dog.Infrastructure
{

    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IErrorLoggingService, ErrorLoggingService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IEmailService, SmtpEmailService>();

            

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy("ApiCorsPolicy", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }

        public static IServiceCollection AddPersistanceServicesDependecy(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionstring = configuration.GetConnectionString("DefaultConnection");
            string? hangfireConnectionString = configuration.GetConnectionString("QueueDb");

            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseMySql(connectionstring, ServerVersion.AutoDetect(connectionstring));
                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);
            });

            // Also register as scoped DbContext so ASP.NET Identity works correctly
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySql(connectionstring, ServerVersion.AutoDetect(connectionstring));
                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);
            });

            // Only configure Hangfire if connection string is available
            if (!string.IsNullOrEmpty(hangfireConnectionString))
            {
                services.AddHangfire(config => config
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseStorage(new MySqlStorage(hangfireConnectionString, new MySqlStorageOptions
                    {
                        QueuePollInterval = TimeSpan.FromSeconds(10),
                        JobExpirationCheckInterval = TimeSpan.FromHours(1),
                        CountersAggregateInterval = TimeSpan.FromMinutes(5),
                        PrepareSchemaIfNecessary = true,
                        DashboardJobListLimit = 25000,
                        TransactionTimeout = TimeSpan.FromMinutes(1),
                        TablesPrefix = "Hangfire",
                    })));

                services.AddHangfireServer();
            }

            services.AddIdentity<ApplicationUser, Role>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // HttpClient for webhook delivery
            services.AddHttpClient("WebhookClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // HttpClient for geo/location API calls
            services.AddHttpClient("GeoClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            return services;
        }
    }

}
