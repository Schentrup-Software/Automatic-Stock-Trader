using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Stonks2.Alpaca;
using Stonks2.Configuration;
using Stonks2.Stratagies;
using Stonks2.Stratagies.MeanReversionStrategy;
using Stonks2.Stratagies.MicrotrendStrategy;
using Stonks2.Stratagies.MLStrategy;
using Stonks2.Stratagies.NewsStrategy;

class Program
{
    /// <param name="stockSymbol">The stock symbol to employ the trading strategy on.</param>
    /// <param name="strategyNames">A  comma seperated list of the strategies to employ on the stock. Possible list list of strategies: MeanReversionStrategy, MLStrategy, and MicrotrendStrategy.</param>
    static async Task<int> Main(string stockSymbol = "AAPL", string[] strategyNames = null)
    {
        //Config
        var config = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", true, true)
            .AddEnvironmentVariables()
            .Build();

        var alpacaConfig = config.Get<AlpacaConfig>();
        var alpacaClient = new AlpacaClient(alpacaConfig);

        var strategies = new List<Strategy>()
        {
            new MeanReversionStrategy(alpacaClient),
            new MLStrategy(alpacaClient, config.Get<MLConfig>()),
            //new NewsStrategy(alpacaClient, config.Get<NewsSearchConfig>()),
            new MicrotrendStrategy(alpacaClient),
        };

        strategies = strategies
            .Where(x => strategyNames
                .Any(y => string.Equals(x.GetType().Name, y, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (strategies.Count == 0)
        {
            throw new ArgumentException("Could not find any strategies with the names"
                + $"'{strategyNames.Aggregate((x, y) => $"{x}, {y}")}'", nameof(strategyNames));
        }

        var stockData = await alpacaClient.GetStockData(stockSymbol);

        if (!await alpacaClient.ConnectStreamApi(stockSymbol))
        {
            throw new UnauthorizedAccessException("Failed to connect to streaming API. Authorization failed.");
        }

        strategies.ForEach(x => {
            x.HistoricalData = stockData;
            alpacaClient.SubscribeMinuteAgg(stockSymbol, async y => await x.HandleMinuteAgg(y));
        });

        while (true)
        {
            await Task.Delay(600000);
        }
    }
}
