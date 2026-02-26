using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneIncTask.Application.Services;
using OneIncTask.Domain.Enums;
using OneIncTask.Domain.Repositories;

namespace OneIncTask.Infrastructure.BackgroundServices;

public class JobProcessorBackgroundService(
    Channel<JobProcessingRequest> jobChannel,
    IServiceScopeFactory scopeFactory,
    JobCancellationRegistry cancellationRegistry,
    ILogger<JobProcessorBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Job Processor Background Service started");

        await foreach (var request in jobChannel.Reader.ReadAllAsync(stoppingToken))
        {
            _ = ProcessJobAsync(request, stoppingToken);
        }

        logger.LogInformation("Job Processor Background Service stopped");
    }

    private async Task ProcessJobAsync(JobProcessingRequest request, CancellationToken stoppingToken)
    {
        using var jobCts = cancellationRegistry.Register(request.JobId);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(jobCts.Token, stoppingToken);

        try
        {
            logger.LogInformation("Processing job {JobId} for user {UserId}", request.JobId, request.UserId);

            using var scope = scopeFactory.CreateScope();
            var jobProcessingService = scope.ServiceProvider.GetRequiredService<IJobProcessingService>();

            await jobProcessingService.ProcessJobAsync(
                request.JobId, request.UserId, request.InputText, linkedCts.Token);

            logger.LogInformation("Job {JobId} completed successfully", request.JobId);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Job {JobId} was cancelled", request.JobId);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var jobWriteRepository = scope.ServiceProvider.GetRequiredService<IJobWriteRepository>();
                var jobReadRepository = scope.ServiceProvider.GetRequiredService<IJobReadRepository>();
                var notifier = scope.ServiceProvider.GetRequiredService<IJobProgressNotifier>();

                var job = await jobReadRepository.GetByIdAsync(request.JobId);
                if (job != null && job.Status != JobStatus.Cancelled)
                {
                    job.Status = JobStatus.Cancelled;
                    job.CompletedAt = DateTime.UtcNow;
                    await jobWriteRepository.UpdateAsync(job);
                    await notifier.NotifyJobCancelled(request.UserId, request.JobId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating cancelled job {JobId}", request.JobId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed with error", request.JobId);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var jobWriteRepository = scope.ServiceProvider.GetRequiredService<IJobWriteRepository>();
                var jobReadRepository = scope.ServiceProvider.GetRequiredService<IJobReadRepository>();
                var notifier = scope.ServiceProvider.GetRequiredService<IJobProgressNotifier>();

                var job = await jobReadRepository.GetByIdAsync(request.JobId);
                if (job != null)
                {
                    job.Status = JobStatus.Failed;
                    job.ErrorMessage = ex.Message;
                    job.CompletedAt = DateTime.UtcNow;
                    await jobWriteRepository.UpdateAsync(job);
                    await notifier.NotifyJobFailed(request.UserId, request.JobId, ex.Message);
                }
            }
            catch (Exception innerEx)
            {
                logger.LogError(innerEx, "Error updating failed job {JobId}", request.JobId);
            }
        }
        finally
        {
            cancellationRegistry.Unregister(request.JobId);
        }
    }
}
