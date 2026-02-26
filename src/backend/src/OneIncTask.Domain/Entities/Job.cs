using OneIncTask.Domain.Common;
using OneIncTask.Domain.Enums;

namespace OneIncTask.Domain.Entities;

public class Job : BaseEntity
{
    public required string UserId { get; set; }
    public required string InputText { get; set; }
    public string? ExpectedResult { get; set; }
    public string CurrentResult { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public int TotalCharacters { get; set; }
    public int ProcessedCharacters { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
