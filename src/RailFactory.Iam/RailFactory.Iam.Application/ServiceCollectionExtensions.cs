using Microsoft.Extensions.DependencyInjection;
using RailFactory.Iam.Application.Services;

namespace RailFactory.Iam.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIamApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserApplicationService, UserApplicationService>();
        return services;
    }
}
