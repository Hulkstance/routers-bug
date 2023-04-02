using Proto;
using Proto.DependencyInjection;

namespace Test.Proto;

public static class ActorSystemConfiguration
{
    public static IServiceCollection AddActorSystem(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(provider =>
        {
            Log.SetLoggerFactory(provider.GetRequiredService<ILoggerFactory>());

            var actorSystemConfig = ActorSystemConfig
                .Setup()
                .WithDeadLetterThrottleCount(3)
                .WithDeadLetterThrottleInterval(TimeSpan.FromSeconds(1));

            return new ActorSystem(actorSystemConfig)
                .WithServiceProvider(provider);
        });

        return services;
    }
}
