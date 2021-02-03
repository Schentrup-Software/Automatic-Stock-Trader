using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Repository;
using AutomaticStockTrader.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AutomaticStockTrader
{
    public class StartAutoTrade : IHostedService
    {
        private readonly IAlpacaClient _alpacaClient;
        private readonly StockContext _context;
        private readonly IEnumerable<StrategyHandler> _strategies;
        private readonly ITrackingRepository _trackingRepository;


        private readonly StockConfig _stockConfig;
        private readonly StrategyConfig _strategyConfig;

        public StartAutoTrade(
            IAlpacaClient alpacaClient,
            StockContext context,
            IOptions<StockConfig> stockConfig,
            IOptions<StrategyConfig> strategyConfig,
            IEnumerable<StrategyHandler> strategies,
            ITrackingRepository trackingRepository
        )
        {
            _alpacaClient = alpacaClient ?? throw new ArgumentNullException(nameof(alpacaClient));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
            _trackingRepository = trackingRepository ?? throw new ArgumentNullException(nameof(trackingRepository));

            _stockConfig = stockConfig?.Value ?? throw new ArgumentNullException(nameof(stockConfig));
            _strategyConfig = strategyConfig?.Value ?? throw new ArgumentNullException(nameof(strategyConfig));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_context.IsUsingDefault)
            {
                await _context.Database.EnsureCreatedAsync(cancellationToken);              
            }
            else
            {
                await _context.Database.MigrateAsync(cancellationToken: cancellationToken);
            }

            if ((_stockConfig.Stock_List?.Count ?? 0) == 0)
            {
                throw new ArgumentException("You must pass an least one valid stock symbol", nameof(_stockConfig));
            }       

            if (!_strategies.Any())
            {
                throw new ArgumentException($"No strategies given. You must provide at least on strategy.", nameof(_stockConfig));
            }

            if (!await _alpacaClient.ConnectStreamApi())
            {
                throw new UnauthorizedAccessException("Failed to connect to streaming API. Authorization failed.");
            }

            _alpacaClient.SubscribeToTradeUpdates(async order => await _trackingRepository.CompleteOrder(order.StockSymbol, order.MarketPrice, order.SharesBought));

            foreach (var strategy in _strategies)
            {
                foreach (var stockSymbol in _stockConfig.Stock_List)
                {
                    var stockData = await _alpacaClient.GetStockData(stockSymbol);

                    if ((stockData?.Count ?? 0) == 0)
                    {
                        throw new ArgumentException($"You stock symbol {stockSymbol} is not valid.", nameof(_stockConfig));
                    }

                    strategy.HistoricalData.AddRange(stockData);
                    _alpacaClient.SubscribeMinuteAgg(stockSymbol, async y => await strategy.HandleMinuteAgg(y));
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
