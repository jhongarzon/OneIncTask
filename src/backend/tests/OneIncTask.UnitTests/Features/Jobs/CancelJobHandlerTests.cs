using FluentAssertions;
using Moq;
using OneIncTask.Application.Features.Jobs.Commands;
using OneIncTask.Application.Services;
using OneIncTask.Domain.Entities;
using OneIncTask.Domain.Enums;
using OneIncTask.Domain.Repositories;

namespace OneIncTask.UnitTests.Features.Jobs;

public class CancelJobHandlerTests
{
    private readonly Mock<IJobReadRepository> _jobReadRepositoryMock = new();
    private readonly Mock<IJobWriteRepository> _jobWriteRepositoryMock = new();
    private readonly JobCancellationRegistry _cancellationRegistry = new();
    private readonly Mock<IJobProgressNotifier> _progressNotifierMock = new();
    private readonly CancelJob.Handler _handler;

    public CancelJobHandlerTests()
    {
        _handler = new CancelJob.Handler(
            _jobReadRepositoryMock.Object,
            _jobWriteRepositoryMock.Object,
            _cancellationRegistry,
            _progressNotifierMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCancelRequest_CancelsJob()
    {
        var jobId = Guid.NewGuid();
        var job = new Job { UserId = "user1", InputText = "test", Status = JobStatus.Running };
        typeof(OneIncTask.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(job, jobId);

        _jobReadRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new CancelJob.CancelJobRequest(jobId, "user1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _jobWriteRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Job>(j => j.Status == JobStatus.Cancelled), It.IsAny<CancellationToken>()), Times.Once);
        _progressNotifierMock.Verify(n => n.NotifyJobCancelled("user1", jobId), Times.Once);
    }

    [Fact]
    public async Task Handle_JobNotFound_ReturnsFailure()
    {
        var jobId = Guid.NewGuid();
        _jobReadRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var result = await _handler.Handle(new CancelJob.CancelJobRequest(jobId, "user1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WrongUser_ReturnsFailure()
    {
        var jobId = Guid.NewGuid();
        var job = new Job { UserId = "user1", InputText = "test", Status = JobStatus.Running };
        typeof(OneIncTask.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(job, jobId);

        _jobReadRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new CancelJob.CancelJobRequest(jobId, "user2"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CompletedJob_ReturnsFailure()
    {
        var jobId = Guid.NewGuid();
        var job = new Job { UserId = "user1", InputText = "test", Status = JobStatus.Completed };
        typeof(OneIncTask.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(job, jobId);

        _jobReadRepositoryMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var result = await _handler.Handle(new CancelJob.CancelJobRequest(jobId, "user1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
