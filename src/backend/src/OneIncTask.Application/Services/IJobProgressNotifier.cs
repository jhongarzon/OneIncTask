namespace OneIncTask.Application.Services;

public interface IJobProgressNotifier
{
    Task NotifyJobStarted(string userId, Guid jobId, int totalCharacters);
    Task NotifyCharacterProcessed(string userId, Guid jobId, char character, int currentIndex, int totalCount);
    Task NotifyJobCompleted(string userId, Guid jobId, string fullResult);
    Task NotifyJobCancelled(string userId, Guid jobId);
    Task NotifyJobFailed(string userId, Guid jobId, string error);
}
