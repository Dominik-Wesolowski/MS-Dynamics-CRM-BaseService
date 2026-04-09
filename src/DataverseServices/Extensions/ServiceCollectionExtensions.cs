using DataverseServices.Abstractions;
using DataverseServices.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DataverseServices.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataverseServices(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddScoped<IService, Service>();
        services.AddScoped<IBulkService, BulkService>();

        return services;
    }
}
