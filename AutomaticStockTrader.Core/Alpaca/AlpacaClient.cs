using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpaca.Markets;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Domain;
using Microsoft.Extensions.Options;

namespace AutomaticStockTrader.Core.Alpaca
{
    public class AlpacaClient : IAlpacaClient
    {
        private readonly AlpacaConfig _config;
        private readonly IAlpacaTradingClient _alpacaTradingClient;
        private readonly IAlpacaStreamingClient _alpacaTradingStreamingClient;
        private readonly IAlpacaDataClient _alpacaDataClient;
        private readonly IAlpacaDataStreamingClient _alpacaDataStreamingClient;
        
        private bool disposedValue;
        private IList<IAlpacaDataSubscription<IStreamAgg>> _alpacaDataSubscriptions;

        public AlpacaClient(
            IOptions<AlpacaConfig> config,
            IAlpacaTradingClient alpacaTradingClient,
            IAlpacaStreamingClient alpacaTradingStreamingClient,
            IAlpacaDataClient alpacaDataClient,
            IAlpacaDataStreamingClient alpacaDataStreamingClient
            )
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _alpacaTradingClient = alpacaTradingClient ?? throw new ArgumentNullException(nameof(alpacaTradingClient));
            _alpacaTradingStreamingClient = alpacaTradingStreamingClient ?? throw new ArgumentNullException(nameof(alpacaTradingStreamingClient));
            _alpacaDataClient = alpacaDataClient ?? throw new ArgumentNullException(nameof(alpacaDataClient));
            _alpacaDataStreamingClient = alpacaDataStreamingClient ?? throw new ArgumentNullException(nameof(alpacaDataStreamingClient));

            _alpacaDataSubscriptions = new List<IAlpacaDataSubscription<IStreamAgg>>();
        }

        public async Task<bool> ConnectStreamApis()
        { 
            var dataTask = _alpacaDataStreamingClient.ConnectAndAuthenticateAsync();
            var tradingTask = _alpacaTradingStreamingClient.ConnectAndAuthenticateAsync();

            return (await dataTask) == AuthStatus.Authorized && (await tradingTask) == AuthStatus.Authorized;
        }

        public void AddPendingMinuteAggSubscription(string stockSymbol, Action<StockInput> action)
        {
            void convertedAction(IStreamAgg newValue) => action(new StockInput
            {
                ClosingPrice = newValue.Close,
                Time = newValue.EndTimeUtc,
                StockSymbol = stockSymbol
            });

            var newClient = _alpacaDataStreamingClient.GetMinuteAggSubscription(stockSymbol);
            newClient.Received += convertedAction;
            _alpacaDataSubscriptions.Add(newClient); 
        }

        public void SubscribeToMinuteAgg()
            => _alpacaDataStreamingClient.Subscribe(_alpacaDataSubscriptions);

        public void SubscribeToTradeUpdates(Action<CompletedOrder> action)
        {
            _alpacaTradingStreamingClient.OnTradeUpdate += (trade) =>
            {
                if (trade.Order.OrderStatus == OrderStatus.Filled || trade.Order.OrderStatus == OrderStatus.PartiallyFilled)
                {
                    action(new CompletedOrder
                    {
                        MarketPrice = trade.Price.Value,
                        OrderPlacedTime = trade.TimestampUtc ?? DateTime.UtcNow,
                        SharesBought = trade.Order.OrderSide == OrderSide.Buy ? trade.Order.FilledQuantity : trade.Order.FilledQuantity * (-1),
                        StockSymbol = trade.Order.Symbol
                    });
                }
            };
        }

        /// <summary>
        /// Buy or sell stock
        /// </summary>
        public Task PlaceOrder(StrategysStock strategy, Order order)
            => _alpacaTradingClient.PostOrderAsync(
                new NewOrderRequest(
                    symbol: strategy.StockSymbol,
                    quantity: order.SharesBought > 0 ? order.SharesBought : order.SharesBought * (-1),
                    side: order.SharesBought > 0 ? OrderSide.Buy : OrderSide.Sell,
                    type: OrderType.Market,
                    duration: TimeInForce.Ioc));
        

        /// <summary>
        /// Clear out any postion that exists
        /// </summary>
        public async Task EnsurePostionCleared(StrategysStock postion)
        {
            var actualPostion = (await _alpacaTradingClient.ListPositionsAsync()).SingleOrDefault(x => string.Equals(x.Symbol, postion.StockSymbol, StringComparison.OrdinalIgnoreCase));
            
            if (actualPostion != null)
            {
                await _alpacaTradingClient.PostOrderAsync(new NewOrderRequest(actualPostion.Symbol, actualPostion.Quantity, OrderSide.Sell, OrderType.Market, TimeInForce.Ioc));
            }
        }

        public async Task<decimal> GetTotalEquity()
            => (await _alpacaTradingClient.GetAccountAsync()).Equity;
        

        /// <summary>
        /// Get stock data for single symbol
        /// </summary>
        /// <param name="stockSymbol">Stock symbol to get data for</param>
        /// <returns>Tuple with training data and test data in that order</returns>
        public async Task<IList<StockInput>> GetStockData(string stockSymbol)
        {
            var stockData = await _alpacaDataClient.GetBarSetAsync(new BarSetRequest(stockSymbol, (TimeFrame)_config.Aggregation_Period_Unit)
            {
                Limit = _config.Number_Of_Minutes_To_Look_Back
            });

            return GetStockInputs(stockSymbol, stockData[stockSymbol]);
        }

        private static IList<StockInput> GetStockInputs(string stockSymbol, IEnumerable<IAgg> aggs) => aggs
            .Select((x, i) => new StockInput
            {
                Time = x.TimeUtc ?? throw new ArgumentNullException(nameof(x.TimeUtc)),
                ClosingPrice = x.Close,
                StockSymbol = stockSymbol
            })
            .ToList();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _alpacaDataClient?.Dispose();
                    _alpacaTradingClient?.Dispose();
                    _alpacaDataStreamingClient?.Dispose();
                    _alpacaTradingStreamingClient?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
