using Microsoft.AspNetCore.Http;
using OneIncTask.Application.Services;
using OneIncTask.Domain.Abstractions;
using OneIncTask.Domain.Common;
using OneIncTask.Domain.Enums;
using OneIncTask.Domain.Repositories;

namespace OneIncTask.Application.Features.Jobs.Commands;

public static class CancelJob
{
    public record CancelJobRequest(Guid JobId, string UserId) : ICommand<ApiResponse<bool>>;

    public class Handler(
        IJobReadRepository jobReadRepository,
        IJobWriteRepository jobWriteRepository,
        JobCancellationRegistry cancellationRegistry,
        IJobProgressNotifier progressNotifier) : ICommandHandler<CancelJobRequest, ApiResponse<bool>>
    {
        public async Task<ApiResponse<bool>> Handle(CancelJobRequest command, CancellationToken cancellationToken)
        {
            var job = await jobReadRepository.GetByIdAsync(command.JobId, cancellationToken);

            if (job == null)
                return ApiResponse<bool>.Failure(Results.NotFound($"Job {command.JobId} not found."));

            if (job.UserId != command.UserId)
                return ApiResponse<bool>.Failure(Results.Forbid());

            if (job.Status != JobStatus.Pending && job.Status != JobStatus.Running)
                return ApiResponse<bool>.Failure(
                    Results.BadRequest($"Job cannot be cancelled in {job.Status} state."));

            cancellationRegistry.TryCancel(command.JobId);

            job.Status = JobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
            await jobWriteRepository.UpdateAsync(job, cancellationToken);

            await progressNotifier.NotifyJobCancelled(command.UserId, command.JobId);

            return ApiResponse<bool>.Success(true);
        }
    }
}
