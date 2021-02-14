using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Repository;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Core
{
    public class ScheduledTrader : IJob
    {
        private readonly IAlpacaClient _alpacaClient;
        private readonly IEnumerable<StrategyHandler> _strategies;
        private readonly ITrackingRepository _trackingRepository;

        public ScheduledTrader(
            ITrackingRepository trackingRepository,
            IAlpacaClient alpacaClient,
            IEnumerable<StrategyHandler> strategies)
        {
            _trackingRepository = trackingRepository ?? throw new ArgumentNullException(nameof(trackingRepository));
            _alpacaClient = alpacaClient ?? throw new ArgumentNullException(nameof(alpacaClient));
            _strategies = strategies?.Where(x => x.StockStrategy.TradingFrequency != Domain.TradingFrequency.Minute) ?? throw new ArgumentNullException(nameof(strategies));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            foreach (var strategy in _strategies)
            {
                var stockData = await _alpacaClient.GetStockData(strategy.StockStrategy.StockSymbol);

                if ((stockData?.Count ?? 0) == 0)
                {
                    throw new ArgumentException($"You stock symbol {strategy.StockStrategy.StockSymbol} is not valid.");
                }

                var lastCompletedOrder = _trackingRepository.GetCompletedOrders(strategy.StockStrategy).Max(x => x.OrderPlacedTime);
                var waitTime = lastCompletedOrder.AddDays(1).AddMinutes(1) - DateTime.UtcNow;
                
                if(waitTime.TotalMilliseconds > 0)
                {
                    await Task.Delay((int)Math.Ceiling(waitTime.TotalMilliseconds));
                }
                
                await strategy.RunStrategy(stockData);
            }
        }
    }
}
