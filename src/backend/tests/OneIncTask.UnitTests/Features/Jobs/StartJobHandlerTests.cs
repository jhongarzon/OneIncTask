using FluentAssertions;
using Moq;
using OneIncTask.Application.Features.Jobs.Commands;
using OneIncTask.Application.Services;
using OneIncTask.Domain.Entities;
using OneIncTask.Domain.Repositories;
using System.Threading.Channels;

namespace OneIncTask.UnitTests.Features.Jobs;

public class StartJobHandlerTests
{
    private readonly Mock<IJobReadRepository> _jobReadRepositoryMock = new();
    private readonly Mock<IJobWriteRepository> _jobWriteRepositoryMock = new();
    private readonly Mock<IStringProcessingService> _stringProcessingServiceMock = new();
    private readonly Channel<JobProcessingRequest> _channel = Channel.CreateUnbounded<JobProcessingRequest>();
    private readonly StartJob.Handler _handler;

    public StartJobHandlerTests()
    {
        _handler = new StartJob.Handler(
            _jobReadRepositoryMock.Object,
            _jobWriteRepositoryMock.Object,
            _stringProcessingServiceMock.Object,
            _channel);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesJobAndEnqueues()
    {
        var request = new StartJob.StartJobRequest("user1", "Hello");

        _jobReadRepositoryMock.Setup(r => r.GetActiveJobByUserIdAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        _stringProcessingServiceMock.Setup(s => s.BuildProcessedString("Hello"))
            .Returns("H1e1l2o1/SGVsbG8=");

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.JobId.Should().NotBeEmpty();
        _jobWriteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ActiveJobExists_ReturnsConflict()
    {
        var request = new StartJob.StartJobRequest("user1", "Hello");

        _jobReadRepositoryMock.Setup(r => r.GetActiveJobByUserIdAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Job { UserId = "user1", InputText = "test" });

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _jobWriteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Validator_EmptyInputText_ShouldHaveError()
    {
        var validator = new StartJob.Validator();
        var request = new StartJob.StartJobRequest("user1", "");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InputText");
    }

    [Fact]
    public void Validator_TooLongInputText_ShouldHaveError()
    {
        var validator = new StartJob.Validator();
        var request = new StartJob.StartJobRequest("user1", new string('a', 10001));

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InputText");
    }

    [Fact]
    public void Validator_EmptyUserId_ShouldHaveError()
    {
        var validator = new StartJob.Validator();
        var request = new StartJob.StartJobRequest("", "Hello");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}
