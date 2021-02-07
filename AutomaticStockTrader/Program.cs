using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Repository.Configuration;
using AutomaticStockTrader.Core;
using AutomaticStockTrader.Repository.Models;
using AutomaticStockTrader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Core.Strategies.MeanReversionStrategy;
using AutomaticStockTrader.Core.Strategies.MLStrategy;
using AutomaticStockTrader.Core.Strategies.MicrotrendStrategy;
using Microsoft.Extensions.Options;
using AutomaticStockTrader.Repository;
using Microsoft.Extensions.Logging;
using Alpaca.Markets;
using Environments = Alpaca.Markets.Environments;
using Microsoft.Extensions.Configuration;

class Program
{
    /// <param name="stockSymbols">A list of the stock symbols to employ the trading strategies on.</param>
    /// <param name="strategyNames">A list of strategies to employ on the stock. Possible list of strategies: MeanReversionStrategy, MLStrategy, and MicrotrendStrategy.</param>
    /// <param name="tradingFreqencies">A list of frequencies to run the stragies. The list associates the position of the frequency to the strategy in that same position. Possible values are Minute, Hour, Day</param>
    static async Task<int> Main(string[] stockSymbols = null, string[] strategyNames = null, string[] tradingFreqencies = null)
    {
        Console.WriteLine("App starting");

        var args = new List<string>();

        if (strategyNames?.Any(x => !string.IsNullOrWhiteSpace(x)) ?? false)
        {
            args.Add("--Trading_Strategies_Raw");
            args.Add(strategyNames.Aggregate((x, y) => $"{x}, {y}"));
        }

        if (tradingFreqencies?.Any(x => !string.IsNullOrWhiteSpace(x)) ?? false)
        {
            args.Add("--Trading_Freqencies_Raw");
            args.Add(tradingFreqencies.Aggregate((x, y) => $"{x}, {y}"));
        }

        if (stockSymbols?.Any(x => !string.IsNullOrWhiteSpace(x)) ?? false)
        {
            args.Add("--Stock_List_Raw");
            args.Add(stockSymbols.Aggregate((x, y) => $"{x}, {y}"));
        }

        await CreateHostBuilder(args).RunConsoleAsync();

        return 0;
    }

    private static IHostBuilder CreateHostBuilder(List<string> args) =>
        Host.CreateDefaultBuilder(args.ToArray())
            .ConfigureServices((hostContext, services) =>
            {
                var Config = hostContext.Configuration;

                services
                    .AddOptions()
                    .Configure<AlpacaConfig>(Config)
                    .Configure<StockConfig>(Config)
                    .Configure<StrategyConfig>(Config)
                    .Configure<MLConfig>(Config)
                    .Configure<DatabaseConfig>(Config);

                var (env, key) = GetAlpacaConfig(Config.Get<AlpacaConfig>());

                services
                    .AddHostedService<StartAutoTrade>()
                    .AddDbContext<StockContext>(ServiceLifetime.Transient)
                    .AddSingleton(x => env.GetAlpacaTradingClient(key))
                    .AddSingleton(x => env.GetAlpacaStreamingClient(key))
                    .AddSingleton(x => env.GetAlpacaDataClient(key))
                    .AddSingleton(x => env.GetAlpacaDataStreamingClient(key))
                    .AddSingleton<IAlpacaClient, AlpacaClient>()
                    .AddTransient<ITrackingRepository, TrackingRepository>()
                    .AddTransient(services => {
                        var config = services.GetRequiredService<IOptions<StrategyConfig>>().Value;
                        var stockConfig = services.GetRequiredService<IOptions<StockConfig>>().Value;

                        return config.Trading_Strategies
                            .Select((x, i) => x switch
                            {
                                nameof(MeanReversionStrategy) => 
                                    stockConfig.Stock_List.Select(stockSymbol => 
                                        new StrategyHandler(
                                            services.GetRequiredService<ILogger<StrategyHandler>>(),
                                            services.GetRequiredService<IAlpacaClient>(),
                                            services.GetRequiredService<ITrackingRepository>(),
                                            new MeanReversionStrategy(),
                                            config.Trading_Freqencies.ElementAtOrDefault(i),
                                            config.Percentage_Of_Equity_Per_Position,
                                            stockSymbol)
                                        ),
                                nameof(MLStrategy) =>
                                    stockConfig.Stock_List.Select(stockSymbol => 
                                        new StrategyHandler(
                                            services.GetRequiredService<ILogger<StrategyHandler>>(),
                                            services.GetRequiredService<IAlpacaClient>(),
                                            services.GetRequiredService<ITrackingRepository>(),
                                            new MLStrategy(services.GetRequiredService<IOptions<MLConfig>>().Value),
                                            config.Trading_Freqencies.ElementAtOrDefault(i),
                                            config.Percentage_Of_Equity_Per_Position,
                                            stockSymbol)
                                        ),
                                nameof(MicrotrendStrategy) =>
                                    stockConfig.Stock_List.Select(stockSymbol =>
                                        new StrategyHandler(
                                            services.GetRequiredService<ILogger<StrategyHandler>>(),
                                            services.GetRequiredService<IAlpacaClient>(),
                                            services.GetRequiredService<ITrackingRepository>(),
                                            new MicrotrendStrategy(),
                                            config.Trading_Freqencies.ElementAtOrDefault(i),
                                            config.Percentage_Of_Equity_Per_Position,
                                            stockSymbol)
                                        ),
                                _ => throw new ArgumentException($"Strategy with name of '{x}' is not valid")
                            })
                            .SelectMany(x => x);
                        }
                    ); 
            });

    private static (IEnvironment env, SecurityKey key) GetAlpacaConfig(AlpacaConfig config)
    {
        var env = config.Alpaca_Use_Live_Api ? Environments.Live : Environments.Paper;
        var key = new SecretKey(config.Alpaca_App_Id, config.Alpaca_Secret_Key);

        return (env, key);
    }
}
