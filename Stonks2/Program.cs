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
    /// <param name="stockSymbolsArgs">A list of the stock symbols to employ the trading strategies on.</param>
    /// <param name="strategyNameArgs">A strategy to employ on the stock. Possible list list of strategies: MeanReversionStrategy, MLStrategy, and MicrotrendStrategy.</param>
    static async Task<int> Main(string[] stockSymbolsArgs = null, string strategyNameArgs = null)
    {
        var args = new Dictionary<string, string>();
        
        if (!string.IsNullOrWhiteSpace(strategyNameArgs))
        {
            args.Add("Stock_Strategy", strategyNameArgs);
        }

        if (stockSymbolsArgs?.Any(x => !string.IsNullOrWhiteSpace(x)) ?? false)
        {
            args.Add("Stock_List_Raw", stockSymbolsArgs.Aggregate((x, y) => $"{x}, {y}"));
        }

        //Config
        var config = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", true, true)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(args)
            .Build();

        var alpacaConfig = config.Get<AlpacaConfig>();
        var alpacaClient = new AlpacaClient(alpacaConfig);
        var stockConfig = config.Get<StockConfig>();

        if ((stockConfig.Stock_List?.Count ?? 0) == 0)
        {
            throw new ArgumentException("You must pass an least one valid stock symbol", nameof(stockSymbolsArgs));
        }

        var strategies = new List<Strategy>()
        {
            new MeanReversionStrategy(alpacaClient),
            new MLStrategy(alpacaClient, config.Get<MLConfig>()),
            new MicrotrendStrategy(alpacaClient),
            //new NewsStrategy(alpacaClient, config.Get<NewsSearchConfig>()),
        };

        var strategy = strategies
            .SingleOrDefault(x => string.Equals(x.GetType().Name, stockConfig.Stock_Strategy, StringComparison.OrdinalIgnoreCase));

        if (strategy == null)
        {
            throw new ArgumentException($"Could not find any strategies with the name '{stockConfig.Stock_Strategy}'", nameof(strategyNameArgs));
        }

        if (!await alpacaClient.ConnectStreamApi())
        {
            throw new UnauthorizedAccessException("Failed to connect to streaming API. Authorization failed.");
        }

        foreach(var stockSymbol in stockConfig.Stock_List)
        {
            var stockData = await alpacaClient.GetStockData(stockSymbol);

            if ((stockData?.Count ?? 0) == 0)
            {
                throw new ArgumentException($"You stock symbol {stockSymbol} is not valid.", nameof(stockSymbolsArgs));
            }

            strategy.HistoricalData = stockData;
            alpacaClient.SubscribeMinuteAgg(stockSymbol, async y => await strategy.HandleMinuteAgg(y));
        }

        while (true)
        {
            await Task.Delay(600000);
        }
    }
}
