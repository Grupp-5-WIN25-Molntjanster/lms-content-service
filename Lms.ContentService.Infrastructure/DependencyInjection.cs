using Lms.ContentService.Application.Interfaces;
using Lms.ContentService.Domain.Interfaces;
using Lms.ContentService.Infrastructure.Clients;
using Lms.ContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.ContentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ContentDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("ContentDb")));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ContentDbContext>());
        services.AddScoped<IContentRepository, ContentRepository>();

        services.AddHttpClient<IFileServiceClient, FileServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["ServiceUrls:FileService"]!);
            client.DefaultRequestHeaders.Add("X-Api-Key", configuration["InternalApiKeys:FileService"]);
        });

        services.AddScoped<Lms.ContentService.Application.Services.ContentService>();
        return services;
    }
}