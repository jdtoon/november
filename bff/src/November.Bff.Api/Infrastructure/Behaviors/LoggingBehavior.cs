using Mediator;

namespace November.Bff.Api.Infrastructure.Behaviors;

public sealed class LoggingBehavior<TMessage, TResponse>(
    ILogger<LoggingBehavior<TMessage, TResponse>> logger,
    TimeProvider timeProvider) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TMessage).Name;
        var started = timeProvider.GetTimestamp();
        logger.LogInformation("Handling {Request}", name);

        try
        {
            var response = await next(message, cancellationToken);
            logger.LogInformation("Handled {Request} in {ElapsedMs:0.0} ms",
                name, timeProvider.GetElapsedTime(started).TotalMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogWarning("{Request} failed after {ElapsedMs:0.0} ms with {ExceptionType}",
                name, timeProvider.GetElapsedTime(started).TotalMilliseconds, exception.GetType().Name);
            throw;
        }
    }
}
