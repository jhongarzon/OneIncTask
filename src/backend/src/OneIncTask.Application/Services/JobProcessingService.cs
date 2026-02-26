using OneIncTask.Domain.Enums;
using OneIncTask.Domain.Repositories;

namespace OneIncTask.Application.Services;

public class JobProcessingService(
    IStringProcessingService stringProcessingService,
    IJobWriteRepository jobWriteRepository,
    IJobReadRepository jobReadRepository,
    IJobProgressNotifier progressNotifier) : IJobProcessingService
{
    private static readonly Random Random = new();

    public async Task ProcessJobAsync(Guid jobId, string userId, string inputText, CancellationToken cancellationToken)
    {
        var processedString = stringProcessingService.BuildProcessedString(inputText);
        var totalCharacters = processedString.Length;

        var job = await jobReadRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException($"Job {jobId} not found");

        job.Status = JobStatus.Running;
        job.ExpectedResult = processedString;
        job.TotalCharacters = totalCharacters;
        await jobWriteRepository.UpdateAsync(job, cancellationToken);

        await progressNotifier.NotifyJobStarted(userId, jobId, totalCharacters);

        for (var i = 0; i < totalCharacters; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentChar = processedString[i];

            await progressNotifier.NotifyCharacterProcessed(userId, jobId, currentChar, i, totalCharacters);

            // Update DB every 10 characters or on last character
            if (i % 10 == 0 || i == totalCharacters - 1)
            {
                job = await jobReadRepository.GetByIdAsync(jobId, cancellationToken)
                    ?? throw new InvalidOperationException($"Job {jobId} not found");
                job.CurrentResult = processedString[..(i + 1)];
                job.ProcessedCharacters = i + 1;
                await jobWriteRepository.UpdateAsync(job, cancellationToken);
            }

            // Random delay between 1-5 seconds
            var delay = Random.Next(1000, 5001);
            await Task.Delay(delay, cancellationToken);
        }

        job = await jobReadRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException($"Job {jobId} not found");
        job.Status = JobStatus.Completed;
        job.CurrentResult = processedString;
        job.ProcessedCharacters = totalCharacters;
        job.CompletedAt = DateTime.UtcNow;
        await jobWriteRepository.UpdateAsync(job, cancellationToken);

        await progressNotifier.NotifyJobCompleted(userId, jobId, processedString);
    }
}
