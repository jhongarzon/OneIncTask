using FluentValidation;
using OneIncTask.Domain.Abstractions;

namespace OneIncTask.Application.Services;

public class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(command.GetType());
        var validator = serviceProvider.GetService(validatorType) as IValidator;

        if (validator != null)
        {
            var validationContext = new ValidationContext<object>(command);
            var validationResult = await validator.ValidateAsync(validationContext, cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        var handler = serviceProvider.GetService(handlerType) ??
                      throw new InvalidOperationException($"No handler found for {command.GetType()}");

        return await (Task<TResponse>)handlerType
            .GetMethod("Handle")!
            .Invoke(handler, [command, cancellationToken])!;
    }
}
