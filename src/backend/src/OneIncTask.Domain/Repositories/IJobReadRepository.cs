using OneIncTask.Domain.Abstractions;
using OneIncTask.Domain.Entities;

namespace OneIncTask.Domain.Repositories;

public interface IJobReadRepository : IReadRepository<Job>
{
    Task<Job?> GetActiveJobByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> GetJobsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
