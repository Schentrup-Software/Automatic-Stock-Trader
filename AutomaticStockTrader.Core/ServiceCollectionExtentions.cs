using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Core.Strategies.MeanReversionStrategy;
using AutomaticStockTrader.Core.Strategies.MicrotrendStrategy;
using AutomaticStockTrader.Core.Strategies.MLStrategy;
using AutomaticStockTrader.Repository;
using AutomaticStockTrader.Repository.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using Alpaca.Markets;
using Environments = Alpaca.Markets.Environments;
using Quartz;
using Quartz.Impl.Calendar;

namespace AutomaticStockTrader.Core
{
    public static class ServiceCollectionExtentions
    {
        public static IServiceCollection AddStockAutoTrading(this IServiceCollection services, AlpacaConfig config)
        {
            var (env, key) = GetAlpacaConfig(config);

            services
                .AddDbContext<StockContext>(ServiceLifetime.Transient)
                .AddHostedService<InitStockContext>()
                .AddHostedService<StreamingTrader>()
                .AddSingleton(x => env.GetAlpacaTradingClient(key))
                .AddSingleton(x => env.GetAlpacaStreamingClient(key))
                .AddSingleton(x => env.GetAlpacaDataClient(key))
                .AddSingleton(x => env.GetAlpacaDataStreamingClient(key))
                .AddSingleton<IAlpacaClient, AlpacaClient>()
                .AddTransient<ITrackingRepository, TrackingRepository>()
                .AddTransient<ScheduledTrader>()
                .AddTransient(services =>
                {
                    var strategyConfig = services.GetRequiredService<IOptions<StrategyConfig>>().Value;
                    var stockConfig = services.GetRequiredService<IOptions<StockConfig>>().Value;

                    return strategyConfig.Trading_Strategies
                        .Select((x, i) => x switch
                        {
                            nameof(MeanReversionStrategy) =>
                                stockConfig.Stock_List.Select(stockSymbol =>
                                    new StrategyHandler(
                                        services.GetRequiredService<ILogger<StrategyHandler>>(),
                                        services.GetRequiredService<IAlpacaClient>(),
                                        services.GetRequiredService<ITrackingRepository>(),
                                        new MeanReversionStrategy(),
                                        strategyConfig.Trading_Freqencies.ElementAtOrDefault(i),
                                        strategyConfig.Percentage_Of_Equity_Per_Position,
                                        stockSymbol)
                                    ),
                            nameof(MLStrategy) =>
                                stockConfig.Stock_List.Select(stockSymbol =>
                                    new StrategyHandler(
                                        services.GetRequiredService<ILogger<StrategyHandler>>(),
                                        services.GetRequiredService<IAlpacaClient>(),
                                        services.GetRequiredService<ITrackingRepository>(),
                                        new MLStrategy(services.GetRequiredService<IOptions<MLConfig>>().Value),
                                        strategyConfig.Trading_Freqencies.ElementAtOrDefault(i),
                                        strategyConfig.Percentage_Of_Equity_Per_Position,
                                        stockSymbol)
                                    ),
                            nameof(MicrotrendStrategy) =>
                                stockConfig.Stock_List.Select(stockSymbol =>
                                    new StrategyHandler(
                                        services.GetRequiredService<ILogger<StrategyHandler>>(),
                                        services.GetRequiredService<IAlpacaClient>(),
                                        services.GetRequiredService<ITrackingRepository>(),
                                        new MicrotrendStrategy(),
                                        strategyConfig.Trading_Freqencies.ElementAtOrDefault(i),
                                        strategyConfig.Percentage_Of_Equity_Per_Position,
                                        stockSymbol)
                                    ),
                            _ => throw new ArgumentException($"Strategy with name of '{x}' is not valid")
                        })
                        .SelectMany(x => x);
                });

            var holidayCalendar = new HolidayCalendar();
            services
                .BuildServiceProvider()
                .GetRequiredService<IAlpacaClient>()
                .GetAllTradingHolidays()
                .Result
                .ToList()
                .ForEach(x => holidayCalendar.AddExcludedDate(x));

            services
                .AddQuartzHostedService()
                .AddQuartz(quartz =>
                {
                    quartz.UseMicrosoftDependencyInjectionJobFactory();

                    const string tradingHolidays = "Trading Holidays";
                    quartz
                        .AddCalendar(tradingHolidays, holidayCalendar, true, true)
#if DEBUG
                        .ScheduleJob<ScheduledTrader>(trigger => trigger
                            .StartNow()
                            .WithSimpleSchedule(sched => sched
                                .WithIntervalInSeconds(1)
                                .WithRepeatCount(0)));
#else
                        .ScheduleJob<ScheduledTrader>(trigger => trigger
                            .StartNow()
                            .WithDailyTimeIntervalSchedule(schd => schd
                                .StartingDailyAt(new TimeOfDay(14, 45)))
                            .ModifiedByCalendar(tradingHolidays));
#endif
                });

            return services;
        }

        private static (IEnvironment env, SecurityKey key) GetAlpacaConfig(AlpacaConfig config)
        {
            var env = config.Alpaca_Use_Live_Api ? Environments.Live : Environments.Paper;
            var key = new SecretKey(config.Alpaca_App_Id, config.Alpaca_Secret_Key);

            return (env, key);
        }
    }
}
