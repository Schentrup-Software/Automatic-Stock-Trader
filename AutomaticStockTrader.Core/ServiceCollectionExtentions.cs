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
        private const string EST_STANDARD_NAME = "Eastern Standard Time";

        public static IServiceCollection AddStockAutoTrading(this IServiceCollection services, AlpacaConfig config)
        {
            var (env, key) = GetAlpacaConfig(config);

            services
                .AddDbContext<StockContext>(ServiceLifetime.Transient)
                .AddHostedService<InitStockContext>()
                .AddSingleton(x => env.GetAlpacaTradingClient(key))
                .AddSingleton(x => env.GetAlpacaStreamingClient(key))
                .AddSingleton(x => env.GetAlpacaDataClient(key))
                .AddSingleton(x => env.GetAlpacaDataStreamingClient(key))
                .AddSingleton<IAlpacaClient, AlpacaClient>()
                .AddTransient<ITrackingRepository, TrackingRepository>()
                .AddTransient<ScheduledTrader>()
                .AddTransient<StartStreamingTrader>()
                .AddTransient<CloseStreamingTrader>()
                .AddTransient(services =>
                {
                    var strategyConfig = services.GetRequiredService<IOptions<StrategyConfig>>().Value;

                    return strategyConfig.Trading_Strategies_Parsed
                        .Select((x, i) => x switch
                        {
                            nameof(MeanReversionStrategy) =>
                                strategyConfig.Stock_List_Parsed.Select(stockSymbol =>
                                    new StrategyHandler(
                                        services.GetRequiredService<ILogger<StrategyHandler>>(),
                                        services.GetRequiredService<IAlpacaClient>(),
                                        services.GetRequiredService<ITrackingRepository>(),
                                        new MeanReversionStrategy(),
                                        strategyConfig.Trading_Freqencies_Parsed.ElementAtOrDefault(i),
                                        strategyConfig.Percentage_Of_Equity_Per_Position,
                                        stockSymbol)
                                    ),
                            nameof(MLStrategy) =>
                                strategyConfig.Stock_List_Parsed.Select(stockSymbol =>
                                    new StrategyHandler(
                                        services.GetRequiredService<ILogger<StrategyHandler>>(),
                                        services.GetRequiredService<IAlpacaClient>(),
                                        services.GetRequiredService<ITrackingRepository>(),
                                        new MLStrategy(services.GetRequiredService<IOptions<MLConfig>>().Value),
                                        strategyConfig.Trading_Freqencies_Parsed.ElementAtOrDefault(i),
                                        strategyConfig.Percentage_Of_Equity_Per_Position,
                                        stockSymbol)
                                    ),
                            nameof(MicrotrendStrategy) =>
                                strategyConfig.Stock_List_Parsed.Select(stockSymbol =>
                                    new StrategyHandler(
                                        services.GetRequiredService<ILogger<StrategyHandler>>(),
                                        services.GetRequiredService<IAlpacaClient>(),
                                        services.GetRequiredService<ITrackingRepository>(),
                                        new MicrotrendStrategy(),
                                        strategyConfig.Trading_Freqencies_Parsed.ElementAtOrDefault(i),
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
                .AddQuartz(quartz =>
                {
                    quartz.UseMicrosoftDependencyInjectionJobFactory();

                    const string tradingHolidays = "Trading Holidays";

                    quartz
                        .AddCalendar(tradingHolidays, holidayCalendar, true, true)
                        .ScheduleJob<StartStreamingTrader>(tradingHolidays, config.Trading_Start_Time_Parsed[0], config.Trading_Start_Time_Parsed[1])
                        .ScheduleJob<ScheduledTrader>(tradingHolidays, config.Trading_Start_Time_Parsed[0], config.Trading_Start_Time_Parsed[1])
                        .ScheduleJob<CloseStreamingTrader>(tradingHolidays, config.Trading_Stop_Time_Parsed[0], config.Trading_Stop_Time_Parsed[1]);
                })
                .AddQuartzHostedService();

            return services;
        }

        private static (IEnvironment env, SecurityKey key) GetAlpacaConfig(AlpacaConfig config)
        {
            var env = config.Alpaca_Use_Live_Api ? Environments.Live : Environments.Paper;
            var key = new SecretKey(config.Alpaca_App_Id, config.Alpaca_Secret_Key);

            return (env, key);
        }

        private static IServiceCollectionQuartzConfigurator ScheduleJob<T>(this IServiceCollectionQuartzConfigurator quartz, string holidayCalendar, int hour, int min) where T : IJob
            => quartz
                .ScheduleJob<ScheduledTrader>(trigger => trigger
                    .StartNow()
                    .WithCronSchedule(CronScheduleBuilder
                        .DailyAtHourAndMinute(hour, min)
                        .InTimeZone(TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x => string.Equals(x.StandardName, EST_STANDARD_NAME, StringComparison.InvariantCultureIgnoreCase))) ?? throw new InvalidTimeZoneException($"{EST_STANDARD_NAME} time zone not found. Please install to run application."))
                    .ModifiedByCalendar(holidayCalendar));
    }
}
