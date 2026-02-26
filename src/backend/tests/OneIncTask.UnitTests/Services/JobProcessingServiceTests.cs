using FluentAssertions;
using Moq;
using OneIncTask.Application.Services;
using OneIncTask.Domain.Entities;
using OneIncTask.Domain.Enums;
using OneIncTask.Domain.Repositories;

namespace OneIncTask.UnitTests.Services;

public class JobProcessingServiceTests
{
    private readonly Mock<IStringProcessingService> _stringProcessingServiceMock = new();
    private readonly Mock<IJobWriteRepository> _jobWriteRepositoryMock = new();
    private readonly Mock<IJobReadRepository> _jobReadRepositoryMock = new();
    private readonly Mock<IJobProgressNotifier> _progressNotifierMock = new();
    private readonly JobProcessingService _service;

    public JobProcessingServiceTests()
    {
        _service = new JobProcessingService(
            _stringProcessingServiceMock.Object,
            _jobWriteRepositoryMock.Object,
            _jobReadRepositoryMock.Object,
            _progressNotifierMock.Object);
    }

    [Fact]
    public async Task ProcessJobAsync_CompletesSuccessfully()
    {
        var jobId = Guid.NewGuid();
        var userId = "testuser";
        var input = "ab";
        var processedString = "a1b1/YWI=";

        _stringProcessingServiceMock.Setup(s => s.BuildProcessedString(input))
            .Returns(processedString);

        var job = new Job { UserId = userId, InputText = input, Status = JobStatus.Pending };
        typeof(OneIncTask.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(job, jobId);

        _jobReadRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        await _service.ProcessJobAsync(jobId, userId, input, cts.Token);

        _progressNotifierMock.Verify(n => n.NotifyJobStarted(userId, jobId, processedString.Length), Times.Once);
        _progressNotifierMock.Verify(n => n.NotifyJobCompleted(userId, jobId, processedString), Times.Once);
        _progressNotifierMock.Verify(
            n => n.NotifyCharacterProcessed(userId, jobId, It.IsAny<char>(), It.IsAny<int>(), processedString.Length),
            Times.Exactly(processedString.Length));
    }

    [Fact]
    public async Task ProcessJobAsync_WhenCancelled_ThrowsOperationCancelledException()
    {
        var jobId = Guid.NewGuid();
        var userId = "testuser";
        var input = "Hello, World!";
        var processedString = "long string for testing";

        _stringProcessingServiceMock.Setup(s => s.BuildProcessedString(input))
            .Returns(processedString);

        var job = new Job { UserId = userId, InputText = input, Status = JobStatus.Pending };
        typeof(OneIncTask.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(job, jobId);

        _jobReadRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _service.ProcessJobAsync(jobId, userId, input, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ProcessJobAsync_JobNotFound_ThrowsInvalidOperationException()
    {
        var jobId = Guid.NewGuid();

        _stringProcessingServiceMock.Setup(s => s.BuildProcessedString(It.IsAny<string>()))
            .Returns("test");

        _jobReadRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _service.ProcessJobAsync(jobId, "user", "input", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
