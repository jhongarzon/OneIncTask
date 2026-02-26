using Microsoft.EntityFrameworkCore;
using OneIncTask.Domain.Entities;
using OneIncTask.Domain.Enums;
using OneIncTask.Domain.Repositories;

namespace OneIncTask.Infrastructure.Persistence.Repositories;

public class JobReadRepository(AppDbContext context) : IJobReadRepository
{
    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Job>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Jobs
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Job?> GetActiveJobByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await context.Jobs
            .AsNoTracking()
            .Where(j => j.UserId == userId && (j.Status == JobStatus.Pending || j.Status == JobStatus.Running))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Job>> GetJobsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await context.Jobs
            .AsNoTracking()
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
