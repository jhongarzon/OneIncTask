using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneIncTask.Domain.Repositories;
using OneIncTask.Infrastructure.BackgroundServices;
using OneIncTask.Infrastructure.Persistence;
using OneIncTask.Infrastructure.Persistence.Repositories;

namespace OneIncTask.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IJobReadRepository, JobReadRepository>();
        services.AddScoped<IJobWriteRepository, JobWriteRepository>();

        services.AddHostedService<JobProcessorBackgroundService>();

        return services;
    }
}
