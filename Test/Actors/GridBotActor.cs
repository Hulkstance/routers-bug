using Proto;
using Test.Models;

namespace Test.Actors;

public sealed class GridBotActor : IActor
{
    private readonly ILogger<GridBotActor> _logger;

    public GridBotActor(ILogger<GridBotActor> logger) => _logger = logger;

    public Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Started:
                _logger.LogWarning("Grid bot started");
                break;

            case PriceUpdated { Symbol: var symbol, Price: var newPrice }:
                _logger.LogWarning("Price updated for {Symbol} to {NewPrice}", symbol, newPrice);
                break;

            case string msg:
                _logger.LogError("Message: {Message}", msg);
                break;
        }

        return Task.CompletedTask;
    }
}
