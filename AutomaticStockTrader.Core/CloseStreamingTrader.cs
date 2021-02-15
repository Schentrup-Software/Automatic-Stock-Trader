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
    public class CloseStreamingTrader : IJob
    {
        private readonly IAlpacaClient _alpacaClient;
        private readonly IEnumerable<StrategyHandler> _strategies;
        private readonly ITrackingRepository _trackingRepository;

        public CloseStreamingTrader(
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
            await _alpacaClient.DisconnectStreamApis();

            foreach (var strategy in _strategies.Where(x => x.StockStrategy.TradingFrequency == Domain.TradingFrequency.Minute))
            {
                var position = await _trackingRepository.GetOrCreateEmptyPosition(strategy.StockStrategy);

                if(position.NumberOfShares > 0)
                {
                    await _alpacaClient.PlaceOrder(strategy.StockStrategy, new Domain.Order
                    {
                        MarketPrice = 0, //TODO: Find a better way to handle this
                        OrderPlacedTime = DateTime.UtcNow,
                        SharesBought = position.NumberOfShares * (-1)
                    });
                }
            }
        }
    }
}
