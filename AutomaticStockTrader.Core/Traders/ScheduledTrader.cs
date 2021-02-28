using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Repository;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Core.Traders
{
    public class ScheduledTrader : IJob
    {
        private readonly ILogger<ScheduledTrader> _logger;
        private readonly IAlpacaClient _alpacaClient;
        private readonly IEnumerable<StrategyHandler> _strategies;
        private readonly ITrackingRepository _trackingRepository;

        public ScheduledTrader(
            ILogger<ScheduledTrader> logger,
            ITrackingRepository trackingRepository,
            IAlpacaClient alpacaClient,
            IEnumerable<StrategyHandler> strategies)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trackingRepository = trackingRepository ?? throw new ArgumentNullException(nameof(trackingRepository));
            _alpacaClient = alpacaClient ?? throw new ArgumentNullException(nameof(alpacaClient));
            _strategies = strategies?.Where(x => x.StockStrategy.TradingFrequency != Domain.TradingFrequency.Minute) ?? throw new ArgumentNullException(nameof(strategies));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"Starting {GetType().Name} job");

            foreach (var strategy in _strategies)
            {
                var stockData = await _alpacaClient.GetStockData(strategy.StockStrategy.StockSymbol, strategy.StockStrategy.TradingFrequency);

                if ((stockData?.Count ?? 0) == 0)
                {
                    throw new ArgumentException($"You stock symbol {strategy.StockStrategy.StockSymbol} is not valid.");
                }

                //TODO: Check if pattern day trader is true. If so do the below
                //If not, pull the last order time for a stock from alpaca directly to ensure we do not intra day trade. 
                var orders = _trackingRepository.GetCompletedOrders(strategy.StockStrategy);

                if (orders.Any()) 
                {
                    var lastCompletedOrder = orders.Max(x => x.OrderPlacedTime);
                    var waitTime = lastCompletedOrder.AddDays(1).AddMinutes(1) - DateTime.UtcNow;

                    if (waitTime.TotalMilliseconds > 0)
                    {
                        await Task.Delay((int)Math.Ceiling(waitTime.TotalMilliseconds));
                    }
                }
                
                await strategy.RunStrategy(stockData);
            }

            _logger.LogInformation($"Finished {GetType().Name} job");
        }
    }
}
