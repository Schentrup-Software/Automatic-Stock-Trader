using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Core.Strategies.MeanReversionStrategy;
using AutomaticStockTrader.Core.Strategies.MicrotrendStrategy;
using AutomaticStockTrader.Core.Strategies.MLStrategy;
using AutomaticStockTrader.Repository.Configuration;
using AutomaticStockTrader.Repository;
using AutomaticStockTrader.Repository.Models;
using Microsoft.EntityFrameworkCore;

class Program
{
    /// <param name="stockSymbols">A list of the stock symbols to employ the trading strategies on.</param>
    /// <param name="strategyNames">A list of strategies to employ on the stock. Possible list of strategies: MeanReversionStrategy, MLStrategy, and MicrotrendStrategy.</param>
    /// <param name="tradingFreqencies">A list of frequencies to run the stragies. The list associates the position of the frequency the stragy in that same position.</param>
    static async Task<int> Main(string[] stockSymbols = null, string[] strategyNames = null, string[] tradingFreqencies = null)
    {
        Console.WriteLine("App starting");

        var args = new Dictionary<string, string>();

        if (strategyNames?.Any(x => !string.IsNullOrWhiteSpace(x)) ?? false)
        {
            args.Add("Trading_Strategies_Raw", strategyNames.Aggregate((x, y) => $"{x}, {y}"));
        }

        if (tradingFreqencies?.Any(x => !string.IsNullOrWhiteSpace(x)) ?? false)
        {
            args.Add("Trading_Freqencies_Raw", strategyNames.Aggregate((x, y) => $"{x}, {y}"));
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
        var strategyConfig = config.Get<StrategyConfig>();
        var mlConfig = config.Get<MLConfig>();
        var dbConfig = config.Get<DatabaseConfig>();
        
        using var alpacaClient = new AlpacaClient(alpacaConfig);

        var context = new StockContext();
        context.Database.Migrate();

        using var repo = new TrackingRepository(context);

        await AutoTradeStocks(alpacaClient, repo, stockConfig, mlConfig, strategyConfig);

        while (true)
        {
            await Task.Delay(600000);
        }    
    }

    private static async Task AutoTradeStocks(IAlpacaClient alpacaClient, ITrackingRepository repo, StockConfig stockConfig, MLConfig mlConfig, StrategyConfig strategyConfig)
    {
        if ((stockConfig.Stock_List?.Count ?? 0) == 0)
        {
            throw new ArgumentException("You must pass an least one valid stock symbol", nameof(stockConfig));
        }

        var strategies = strategyConfig.Trading_Strategies
            .Select<string, Strategy>((x, i) => x switch
            {
                nameof(MeanReversionStrategy) => new MeanReversionStrategy(
                    alpacaClient: alpacaClient,
                    trackingRepository: repo,
                    tradingFrequency: strategyConfig.Trading_Freqencies.ElementAtOrDefault(i),
                    percentageOfEquityToAllocate: strategyConfig.Percentage_Of_Equity_Per_Position),
                nameof(MLStrategy) => new MLStrategy(
                    alpacaClient: alpacaClient,
                    trackingRepository: repo,
                    tradingFrequency: strategyConfig.Trading_Freqencies.ElementAtOrDefault(i),
                    percentageOfEquityToAllocate: strategyConfig.Percentage_Of_Equity_Per_Position,
                    config: mlConfig),
                nameof(MicrotrendStrategy) => new MicrotrendStrategy(
                    alpacaClient: alpacaClient,
                    trackingRepository: repo,
                    tradingFrequency: strategyConfig.Trading_Freqencies.ElementAtOrDefault(i),
                    percentageOfEquityToAllocate: strategyConfig.Percentage_Of_Equity_Per_Position),
                /*nameof(NewsStrategy) => new NewsStrategy(
                    client: alpacaClient,
                    trackingRepository: repo,
                    percentageOfEquityToAllocate: strategyConfig.Percentage_Of_Equity_Per_Position,
                    config: NewsSearchConfig),*/
                _ => throw new ArgumentException($"Strategy with name of '{x}' is not valid")
            });

        if (!strategies.Any())
        {
            throw new ArgumentException($"No strategies given. You must provide at least on strategy.", nameof(stockConfig));
        }

        if (!await alpacaClient.ConnectStreamApi())
        {
            throw new UnauthorizedAccessException("Failed to connect to streaming API. Authorization failed.");
        }

        alpacaClient.SubscribeToTradeUpdates(async order => await repo.CompleteOrder(order.StockSymbol, order.MarketPrice, order.SharesBought));

        foreach (var strategy in strategies)
        {
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
}
