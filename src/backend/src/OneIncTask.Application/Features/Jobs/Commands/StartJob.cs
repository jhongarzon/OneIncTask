using FluentValidation;
using Microsoft.AspNetCore.Http;
using OneIncTask.Application.Services;
using OneIncTask.Domain.Abstractions;
using OneIncTask.Domain.Common;
using OneIncTask.Domain.Entities;
using OneIncTask.Domain.Enums;
using OneIncTask.Domain.Repositories;
using System.Threading.Channels;

namespace OneIncTask.Application.Features.Jobs.Commands;

public static class StartJob
{
    public record StartJobRequest(string UserId, string InputText) : ICommand<ApiResponse<StartJobResponse>>;

    public record StartJobResponse(Guid JobId);

    public class Handler(
        IJobReadRepository jobReadRepository,
        IJobWriteRepository jobWriteRepository,
        IStringProcessingService stringProcessingService,
        Channel<JobProcessingRequest> jobChannel) : ICommandHandler<StartJobRequest, ApiResponse<StartJobResponse>>
    {
        public async Task<ApiResponse<StartJobResponse>> Handle(StartJobRequest command, CancellationToken cancellationToken)
        {
            var activeJob = await jobReadRepository.GetActiveJobByUserIdAsync(command.UserId, cancellationToken);
            if (activeJob != null)
            {
                return ApiResponse<StartJobResponse>.Failure(
                    Results.Conflict("You already have an active job. Please wait for it to complete or cancel it."));
            }

            var expectedResult = stringProcessingService.BuildProcessedString(command.InputText);

            var job = new Job
            {
                UserId = command.UserId,
                InputText = command.InputText,
                ExpectedResult = expectedResult,
                TotalCharacters = expectedResult.Length,
                Status = JobStatus.Pending
            };

            await jobWriteRepository.AddAsync(job, cancellationToken);

            await jobChannel.Writer.WriteAsync(
                new JobProcessingRequest(job.Id, command.UserId, command.InputText),
                cancellationToken);

            return ApiResponse<StartJobResponse>.Success(new StartJobResponse(job.Id));
        }
    }

    public class Validator : AbstractValidator<StartJobRequest>
    {
        public Validator()
        {
            RuleFor(x => x.InputText)
                .NotEmpty().WithMessage("Input text is required.")
                .MaximumLength(10000).WithMessage("Input text must not exceed 10000 characters.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required.");
        }
    }
}
