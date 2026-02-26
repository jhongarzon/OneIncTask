using Microsoft.EntityFrameworkCore;
using OneIncTask.Domain.Entities;
using OneIncTask.Domain.Repositories;

namespace OneIncTask.Infrastructure.Persistence.Repositories;

public class JobWriteRepository(AppDbContext context) : IJobWriteRepository
{
    public async Task AddAsync(Job entity, CancellationToken cancellationToken = default)
    {
        await context.Jobs.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Job entity, CancellationToken cancellationToken = default)
    {
        context.ChangeTracker.Clear();
        context.Jobs.Attach(entity);
        context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Job entity, CancellationToken cancellationToken = default)
    {
        context.Jobs.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
