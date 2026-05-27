using Lms.ContentService.Application.Interfaces;
using Lms.ContentService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;

namespace Lms.ContentService.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IFileServiceClient> FileServiceClientMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ============================================================
            // Remove ALL EF Core service descriptors
            // ============================================================
            var efDescriptors = services
                .Where(d => d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(DbContextOptions<ContentDbContext>)
                         || d.ServiceType == typeof(ContentDbContext)
                         || d.ServiceType == typeof(IApplicationDbContext))
                .ToList();

            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            // ============================================================
            // Add InMemory database as the ONLY provider
            // ============================================================
            services.AddDbContext<ContentDbContext>(options =>
            {
                options.UseInMemoryDatabase($"ContentTestDb_{Guid.NewGuid()}");
            });

            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ContentDbContext>());

            // ============================================================
            // Replace FileServiceClient with mock
            // ============================================================
            var fileClientDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IFileServiceClient));
            if (fileClientDescriptor != null)
                services.Remove(fileClientDescriptor);

            services.AddSingleton(FileServiceClientMock.Object);

            FileServiceClientMock
                .Setup(f => f.FileExistsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(true);

            // NOTE: Do NOT call EnsureCreated() here.
            // Program.cs handles it with IsRelational() check.
        });
    }
}