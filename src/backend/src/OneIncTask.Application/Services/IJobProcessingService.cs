namespace OneIncTask.Application.Services;

public interface IJobProcessingService
{
    Task ProcessJobAsync(Guid jobId, string userId, string inputText, CancellationToken cancellationToken);
}
