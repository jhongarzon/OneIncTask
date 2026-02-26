using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OneIncTask.Application.Features.Jobs.Commands;
using OneIncTask.Application.Features.Jobs.Queries;
using OneIncTask.Application.Services;
using OneIncTask.Domain.Abstractions;
using OneIncTask.Domain.Common;

namespace OneIncTask.Application;

public static class CqrsServiceExtensions
{
    public static IServiceCollection AddCqrs(this IServiceCollection services)
    {
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Job Command Handlers
        services.AddScoped<ICommandHandler<StartJob.StartJobRequest,
            ApiResponse<StartJob.StartJobResponse>>, StartJob.Handler>();
        services.AddScoped<ICommandHandler<CancelJob.CancelJobRequest,
            ApiResponse<bool>>, CancelJob.Handler>();

        // Job Query Handlers
        services.AddScoped<IQueryHandler<GetJobStatus.GetJobStatusRequest,
            GetJobStatus.JobStatusResponse>, GetJobStatus.Handler>();
        services.AddScoped<IQueryHandler<GetJobHistory.GetJobHistoryRequest,
            IEnumerable<GetJobHistory.JobHistoryItem>>, GetJobHistory.Handler>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<StartJob.Validator>();

        return services;
    }
}
