using OneIncTask.Domain.Abstractions;
using OneIncTask.Domain.Enums;
using OneIncTask.Domain.Repositories;

namespace OneIncTask.Application.Features.Jobs.Queries;

public static class GetJobHistory
{
    public record GetJobHistoryRequest(string UserId) : IQuery<IEnumerable<JobHistoryItem>>;

    public record JobHistoryItem(
        Guid JobId,
        string InputText,
        JobStatus Status,
        int ProcessedCharacters,
        int TotalCharacters,
        DateTime CreatedAt,
        DateTime? CompletedAt);

    public class Handler(IJobReadRepository jobReadRepository) : IQueryHandler<GetJobHistoryRequest, IEnumerable<JobHistoryItem>>
    {
        public async Task<IEnumerable<JobHistoryItem>?> Handle(GetJobHistoryRequest query, CancellationToken cancellationToken)
        {
            var jobs = await jobReadRepository.GetJobsByUserIdAsync(query.UserId, cancellationToken);

            return jobs.Select(j => new JobHistoryItem(
                j.Id,
                j.InputText,
                j.Status,
                j.ProcessedCharacters,
                j.TotalCharacters,
                j.CreatedAt,
                j.CompletedAt))
                .OrderByDescending(j => j.CreatedAt);
        }
    }
}
