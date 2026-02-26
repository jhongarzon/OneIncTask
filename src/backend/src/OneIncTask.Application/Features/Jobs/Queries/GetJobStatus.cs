using Microsoft.AspNetCore.Http;
using OneIncTask.Domain.Abstractions;
using OneIncTask.Domain.Enums;
using OneIncTask.Domain.Repositories;

namespace OneIncTask.Application.Features.Jobs.Queries;

public static class GetJobStatus
{
    public record GetJobStatusRequest(Guid JobId, string UserId) : IQuery<JobStatusResponse>;

    public record JobStatusResponse(
        Guid JobId,
        JobStatus Status,
        string CurrentResult,
        int ProcessedCharacters,
        int TotalCharacters,
        DateTime CreatedAt,
        DateTime? CompletedAt,
        string? ErrorMessage);

    public class Handler(IJobReadRepository jobReadRepository) : IQueryHandler<GetJobStatusRequest, JobStatusResponse>
    {
        public async Task<JobStatusResponse?> Handle(GetJobStatusRequest query, CancellationToken cancellationToken)
        {
            var job = await jobReadRepository.GetByIdAsync(query.JobId, cancellationToken);

            if (job == null || job.UserId != query.UserId)
                return null;

            return new JobStatusResponse(
                job.Id,
                job.Status,
                job.CurrentResult,
                job.ProcessedCharacters,
                job.TotalCharacters,
                job.CreatedAt,
                job.CompletedAt,
                job.ErrorMessage);
        }
    }
}
