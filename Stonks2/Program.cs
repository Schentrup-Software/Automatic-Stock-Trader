using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AutomaticStockTrader.Alpaca;
using AutomaticStockTrader.Configuration;
using AutomaticStockTrader.Stratagies;
using AutomaticStockTrader.Stratagies.MeanReversionStrategy;
using AutomaticStockTrader.Stratagies.MicrotrendStrategy;
using AutomaticStockTrader.Stratagies.MLStrategy;
using AutomaticStockTrader.Stratagies.NewsStrategy;
using AutomaticStockTrader.Repository.Configuration;
using AutomaticStockTrader.Repository;
using AutomaticStockTrader.Repository.Models;

class Program
{
    /// <param name="stockSymbols">A list of the stock symbols to employ the trading strategies on.</param>
    /// <param name="strategyName">A strategy to employ on the stock. Possible list list of strategies: MeanReversionStrategy, MLStrategy, and MicrotrendStrategy.</param>
    static async Task<int> Main(string[] stockSymbols = null, string strategyName = null)
    {
        var args = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(strategyName))
        {
            args.Add("Stock_Strategy", strategyName);
        }

        if (stockSymbols?.Any(x => !string.IsNullOrWhiteSpace(x)) ?? false)
        {
            args.Add("Stock_List_Raw", stockSymbols.Aggregate((x, y) => $"{x}, {y}"));
        }

        //Config
        var config = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", true, true)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(args)
            .Build();

        var alpacaConfig = config.Get<AlpacaConfig>();
        var stockConfig = config.Get<StockConfig>();
        var mlConfig = config.Get<MLConfig>();
        
        using var alpacaClient = new AlpacaClient(alpacaConfig);

        using var repo = new TrackingRepository(new DatabaseContext(new DatabaseConfig()));

        await repo.AddOrder("Bomb", "GE", DateTime.UtcNow);

        Console.WriteLine(repo.GetOrders().Select(x => $"{x.Id} {x.StockSymbol} {x.Strategy}").Aggregate((x, y) => $"{x}, {y}"));
        return 0;
        /*await AutoTradeStocks(alpacaClient, stockConfig, mlConfig);

        while (true)
        {
            await Task.Delay(600000);
        }
        */
    }

    private static async Task AutoTradeStocks(IAlpacaClient alpacaClient, StockConfig stockConfig, MLConfig mlConfig)
    {
        if ((stockConfig.Stock_List?.Count ?? 0) == 0)
        {
            throw new ArgumentException("You must pass an least one valid stock symbol", nameof(stockConfig));
        }

        var strategies = new List<Strategy>()
        {
            new MeanReversionStrategy(alpacaClient),
            new MLStrategy(alpacaClient, mlConfig),
            new MicrotrendStrategy(alpacaClient),
            //new NewsStrategy(alpacaClient, config.Get<NewsSearchConfig>()),
        };

        var strategy = strategies
            .SingleOrDefault(x => string.Equals(x.GetType().Name, stockConfig.Stock_Strategy, StringComparison.OrdinalIgnoreCase));

        if (strategy == null)
        {
            throw new ArgumentException($"Could not find any strategies with the name '{stockConfig.Stock_Strategy}'", nameof(stockConfig));
        }

        if (!await alpacaClient.ConnectStreamApi())
        {
            throw new UnauthorizedAccessException("Failed to connect to streaming API. Authorization failed.");
        }

        foreach (var stockSymbol in stockConfig.Stock_List)
        {
            var stockData = await alpacaClient.GetStockData(stockSymbol);

            if ((stockData?.Count ?? 0) == 0)
            {
                throw new ArgumentException($"You stock symbol {stockSymbol} is not valid.", nameof(stockConfig));
            }

            strategy.HistoricalData = stockData;
            alpacaClient.SubscribeMinuteAgg(stockSymbol, async y => await strategy.HandleMinuteAgg(y));
        }
    }
}
