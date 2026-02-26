using Microsoft.AspNetCore.SignalR;
using OneIncTask.Application.Services;

namespace OneIncTask.Api.Hubs;

public class SignalRJobProgressNotifier(IHubContext<JobProgressHub, IJobProgressClient> hubContext) : IJobProgressNotifier
{
    public async Task NotifyJobStarted(string userId, Guid jobId, int totalCharacters)
    {
        await hubContext.Clients.Group($"user-{userId}").JobStarted(jobId, totalCharacters);
    }

    public async Task NotifyCharacterProcessed(string userId, Guid jobId, char character, int currentIndex, int totalCount)
    {
        await hubContext.Clients.Group($"user-{userId}").ReceiveCharacter(jobId, character, currentIndex, totalCount);
    }

    public async Task NotifyJobCompleted(string userId, Guid jobId, string fullResult)
    {
        await hubContext.Clients.Group($"user-{userId}").JobCompleted(jobId, fullResult);
    }

    public async Task NotifyJobCancelled(string userId, Guid jobId)
    {
        await hubContext.Clients.Group($"user-{userId}").JobCancelled(jobId);
    }

    public async Task NotifyJobFailed(string userId, Guid jobId, string error)
    {
        await hubContext.Clients.Group($"user-{userId}").JobFailed(jobId, error);
    }
}
