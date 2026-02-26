namespace OneIncTask.Api.Hubs;

public interface IJobProgressClient
{
    Task JobStarted(Guid jobId, int totalCharacters);
    Task ReceiveCharacter(Guid jobId, char character, int currentIndex, int totalCount);
    Task JobCompleted(Guid jobId, string fullResult);
    Task JobCancelled(Guid jobId);
    Task JobFailed(Guid jobId, string error);
}
