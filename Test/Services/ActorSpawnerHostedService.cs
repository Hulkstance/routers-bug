using Proto;
using Proto.Mailbox;
using Proto.Router;
using Proto.Router.Messages;
using Test.Actors;
using Test.Models;

namespace Test.Services;

public sealed class ActorSpawnerHostedService : IHostedService
{
    private const int RetryAttempts = 5;

    private readonly IRootContext _rootContext;
    private readonly IServiceProvider _serviceProvider;

    private PID? _router;

    public ActorSpawnerHostedService(ActorSystem actorSystem, IServiceProvider serviceProvider) => (_rootContext, _serviceProvider) = (actorSystem.Root, serviceProvider);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Create grid bot
        var gridBotProps = Props
            .FromProducer(() => ActivatorUtilities.CreateInstance<GridBotActor>(_serviceProvider))
            .WithGuardianSupervisorStrategy(new OneForOneStrategy((_, _) => SupervisorDirective.Restart, RetryAttempts, null))
            .WithDispatcher(new ThreadPoolDispatcher());

        var gridBot = _rootContext.Spawn(gridBotProps);
        var gridBot2 = _rootContext.Spawn(gridBotProps);

        // Create router
        var routerProps = _rootContext.NewBroadcastGroup();
        var router = _rootContext.Spawn(routerProps);

        // Add routees
        _rootContext.Send(router, new RouterAddRoutee(gridBot));
        _rootContext.Send(router, new RouterAddRoutee(gridBot2));

        // TODO: Comment this out and no more price broadcasts
        var routees = await _rootContext.RequestAsync<Routees>(router, new RouterGetRoutees());
        Console.WriteLine($"We have {routees.Pids.Count} routees at the moment");

        // Send price feed updates
        _rootContext.Send(router, new PriceUpdated("BTCUSDT", 200.5m));

        // Do shit
        _router = router;
    }

    public async Task StopAsync(CancellationToken cancellationToken) => await _rootContext.PoisonAsync(_router!);
}
