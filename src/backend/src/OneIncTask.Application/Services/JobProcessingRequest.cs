namespace OneIncTask.Application.Services;

public record JobProcessingRequest(Guid JobId, string UserId, string InputText);
