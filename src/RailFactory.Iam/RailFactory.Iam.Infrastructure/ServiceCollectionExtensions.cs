using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RailFactory.Iam.Application.Ports;
using RailFactory.Iam.Application.Services;
using RailFactory.Iam.Infrastructure.Google;
using RailFactory.Iam.Infrastructure.Persistence;
using RailFactory.Iam.Infrastructure.Persistence.Outbox;
using RailFactory.Iam.Infrastructure.Persistence.Repositories;

namespace RailFactory.Iam.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers IAM infrastructure: DbContext, repositories, unit of work, outbox, Google auth.
    /// </summary>
    public static IServiceCollection AddIamInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("iamdb")
            ?? throw new InvalidOperationException("Connection string 'iamdb' is not configured.");

        services.AddDbContext<IamDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        // Auth & JWT
        var googleClientId = configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(googleClientId))
            throw new InvalidOperationException("Google:ClientId is not configured. Set the Google:ClientId configuration value.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = "https://accounts.google.com";
                options.Audience = googleClientId;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://accounts.google.com",
                    ValidateAudience = true,
                    ValidAudience = googleClientId,
                    ValidateLifetime = true
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireClaim("role", "Admin"));
        });

        // Google OAuth
        services.Configure<GoogleAuthOptions>(configuration.GetSection(GoogleAuthOptions.SectionName));
        services.AddSingleton<IGoogleAuthProvider, GoogleAuthProvider>();
        services.AddScoped<IOAuthStateStore, DistributedCacheOAuthStateStore>();
        services.AddScoped<IAuthCodeStore, DistributedCacheAuthCodeStore>();

        return services;
    }
}
