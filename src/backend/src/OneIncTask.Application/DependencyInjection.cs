using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using OneIncTask.Application.Services;

namespace OneIncTask.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IStringProcessingService, StringProcessingService>();
        services.AddScoped<IJobProcessingService, JobProcessingService>();
        services.AddSingleton<JobCancellationRegistry>();
        services.AddSingleton(Channel.CreateBounded<JobProcessingRequest>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        }));

        return services;
    }
}
