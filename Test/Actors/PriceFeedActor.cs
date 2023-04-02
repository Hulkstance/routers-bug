using Proto;

namespace Test.Actors;

public sealed class PriceFeedActor : IActor
{
    private readonly ILogger<PriceFeedActor> _logger;
    private readonly Dictionary<string, List<Subscription>> _subscriptions = new();

    public PriceFeedActor(ILogger<PriceFeedActor> logger) => _logger = logger;

    public Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Started:
                _logger.LogDebug("Price feed started");
                break;

            case BroadcastPrice p:
            {
                if (_subscriptions.TryGetValue(p.Symbol, out var subscriptions))
                {
                    foreach (var subscription in subscriptions)
                    {
                        context.Send(subscription.Subscriber, new PriceChange(p.Symbol, p.LastPrice, DateTime.UtcNow));
                    }
                }

                break;
            }

            case Subscribe s:
            {
                if (!_subscriptions.TryGetValue(s.Symbol, out var subscriptions))
                {
                    _subscriptions.Add(s.Symbol, subscriptions = new List<Subscription>());
                }

                subscriptions.Add(new Subscription(s.Symbol, s.Subscriber));

                break;
            }

            case Unsubscribe s:
            {
                if (_subscriptions.TryGetValue(s.Symbol, out var subscriptions))
                {
                    var subscription = subscriptions.FirstOrDefault(a => a.Subscriber.Equals(s.Subscriber));

                    if (subscription != null)
                    {
                        subscriptions.Remove(subscription);
                    }
                }

                break;
            }
        }

        return Task.CompletedTask;
    }
}

public class Subscription
{
    public Subscription(string symbol, PID subscriber)
    {
        Symbol = symbol;
        Subscriber = subscriber;
    }

    public string Symbol { get; }
    public PID Subscriber { get; }
}

public class BroadcastPrice
{
    public BroadcastPrice(string symbol, decimal lastPrice)
    {
        Symbol = symbol;
        LastPrice = lastPrice;
    }

    public string Symbol { get; }
    public decimal LastPrice { get; }
}

public class PriceChange
{
    public PriceChange(string symbol, decimal price, DateTime time)
    {
        Symbol = symbol;
        Price = price;
        Time = time;
    }

    public string Symbol { get; }
    public decimal Price { get; }
    public DateTime Time { get; }
}

public class Subscribe
{
    public Subscribe(string symbol, PID subscriber)
    {
        Symbol = symbol;
        Subscriber = subscriber;
    }

    public string Symbol { get; }
    public PID Subscriber { get; }
}

public class Unsubscribe
{
    public Unsubscribe(string symbol, PID subscriber)
    {
        Symbol = symbol;
        Subscriber = subscriber;
    }

    public string Symbol { get; }
    public PID Subscriber { get; }
}
