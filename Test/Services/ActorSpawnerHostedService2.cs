using Proto;
using Proto.Mailbox;
using Proto.Router;
using Proto.Router.Messages;
using Test.Actors;
using Test.Models;

namespace Test.Services;

public sealed class ActorSpawnerHostedService2 : IHostedService
{
    private const int RetryAttempts = 5;

    private readonly IRootContext _rootContext;
    private readonly IServiceProvider _serviceProvider;

    private readonly HashSet<PID> _pids = new();

    public ActorSpawnerHostedService2(ActorSystem actorSystem, IServiceProvider serviceProvider) => (_rootContext, _serviceProvider) = (actorSystem.Root, serviceProvider);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Create grid bot
        var gridBotProps = Props
            .FromProducer(() => ActivatorUtilities.CreateInstance<GridBotActor>(_serviceProvider))
            .WithGuardianSupervisorStrategy(new OneForOneStrategy((_, _) => SupervisorDirective.Restart, RetryAttempts, null))
            .WithDispatcher(new ThreadPoolDispatcher());
        var gridBot = _rootContext.Spawn(gridBotProps);
        var gridBot2 = _rootContext.Spawn(gridBotProps);

        // Create price feed
        var priceFeedProps = Props
            .FromProducer(() => ActivatorUtilities.CreateInstance<PriceFeedActor>(_serviceProvider))
            .WithGuardianSupervisorStrategy(new OneForOneStrategy((_, _) => SupervisorDirective.Restart, RetryAttempts, null))
            .WithDispatcher(new ThreadPoolDispatcher());
        var priceFeed = _rootContext.Spawn(priceFeedProps);

        _rootContext.Send(priceFeed, new Subscribe("BTCUSDT", gridBot));
        _rootContext.Send(priceFeed, new Subscribe("ETHUSDT", gridBot2));

        // Send price feed updates
        _rootContext.Send(priceFeed, new BroadcastPrice("BTCUSDT", 200.5m));
        _rootContext.Send(priceFeed, new BroadcastPrice("BTCUSDT", 250.7m));
        _rootContext.Send(priceFeed, new BroadcastPrice("ETHUSDT", 50.7m));
        _rootContext.Send(priceFeed, new BroadcastPrice("ETHUSDT", 50.5m));

        // Add pids for the poisoning. Poisoning is required as we need to trigger 'Stopping'.
        _pids.Add(gridBot);
        _pids.Add(gridBot2);
        _pids.Add(priceFeed);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var pid in _pids)
        {
            await _rootContext.PoisonAsync(pid);
        }
    }
}
