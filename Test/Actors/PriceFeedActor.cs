using Proto;

namespace Test.Actors;

public sealed class PriceFeedActor : IActor
{
    private readonly ILogger<GridBotActor> _logger;

    public PriceFeedActor(ILogger<GridBotActor> logger) => _logger = logger;

    public Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Started:
                _logger.LogWarning("Price feed started");
                break;
        }

        return Task.CompletedTask;
    }
}
