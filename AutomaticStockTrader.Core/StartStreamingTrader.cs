using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Repository;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace AutomaticStockTrader
{
    public class StartStreamingTrader : IJob
    {
        private readonly IAlpacaClient _alpacaClient;
        private readonly IEnumerable<StrategyHandler> _strategies;
        private readonly ITrackingRepository _trackingRepository;

        public StartStreamingTrader(
            IAlpacaClient alpacaClient,
            IEnumerable<StrategyHandler> strategies,
            ITrackingRepository trackingRepository
        )
        {
            _alpacaClient = alpacaClient ?? throw new ArgumentNullException(nameof(alpacaClient));
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
            _trackingRepository = trackingRepository ?? throw new ArgumentNullException(nameof(trackingRepository));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (!_strategies.Any())
            {
                throw new ArgumentException($"No strategies given. You must provide at least on strategy.");
            }

            if (!await _alpacaClient.ConnectStreamApis())
            {
                throw new UnauthorizedAccessException("Failed to connect to streaming API. Authorization failed.");
            }

            _alpacaClient.SubscribeToTradeUpdates(async order => await _trackingRepository.CompleteOrder(order));

            foreach (var strategy in _strategies.Where(x => x.StockStrategy.TradingFrequency == Domain.TradingFrequency.Minute))
            {
                var stockData = await _alpacaClient.GetStockData(strategy.StockStrategy.StockSymbol);

                if ((stockData?.Count ?? 0) == 0)
                {
                    throw new ArgumentException($"You stock symbol {strategy.StockStrategy.StockSymbol} is not valid.");
                }

                strategy.HistoricalData.AddRange(stockData);
                _alpacaClient.AddPendingMinuteAggSubscription(strategy.StockStrategy.StockSymbol, async y => await strategy.HandleNewData(y));
            }

            _alpacaClient.SubscribeToMinuteAgg();
        }
    }
}
