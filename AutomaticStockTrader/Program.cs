using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Repository.Configuration;
using AutomaticStockTrader.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            args.Add("--Trading_Strategies");
            args.Add(strategyNames.Aggregate((x, y) => $"{x}, {y}"));
        }

        if (tradingFreqencies?.Any(x => !string.IsNullOrWhiteSpace(x)) ?? false)
        {
            args.Add("--Trading_Freqencies");
            args.Add(tradingFreqencies.Aggregate((x, y) => $"{x}, {y}"));
        }

        if (stockSymbols?.Any(x => !string.IsNullOrWhiteSpace(x)) ?? false)
        {
            args.Add("--Stock_List");
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
                    .Configure<StrategyConfig>(Config)
                    .Configure<MLConfig>(Config)
                    .Configure<DatabaseConfig>(Config);

                services
                    .AddLogging(opt =>
                    {
                        opt.AddSimpleConsole(opt =>
                        {
                            opt.TimestampFormat = "hh:mm:ss ";
                            opt.SingleLine = true;
                        });
                    })
                    .AddStockAutoTrading(Config.Get<AlpacaConfig>());
            });


}
