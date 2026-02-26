using FluentAssertions;
using OneIncTask.Domain.Entities;
using OneIncTask.Domain.Enums;

namespace OneIncTask.UnitTests.Domain;

public class JobEntityTests
{
    [Fact]
    public void NewJob_HasDefaultValues()
    {
        var job = new Job { UserId = "user1", InputText = "test" };

        job.Id.Should().NotBeEmpty();
        job.Status.Should().Be(JobStatus.Pending);
        job.CurrentResult.Should().BeEmpty();
        job.ProcessedCharacters.Should().Be(0);
        job.TotalCharacters.Should().Be(0);
        job.CompletedAt.Should().BeNull();
        job.ErrorMessage.Should().BeNull();
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void JobStatus_HasAllExpectedValues()
    {
        var values = Enum.GetValues<JobStatus>();

        values.Should().HaveCount(5);
        values.Should().Contain(JobStatus.Pending);
        values.Should().Contain(JobStatus.Running);
        values.Should().Contain(JobStatus.Completed);
        values.Should().Contain(JobStatus.Cancelled);
        values.Should().Contain(JobStatus.Failed);
    }
}
