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
        private IDictionary<string, List<Action<StockInput>>> _stockActions;

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

            _stockActions = new Dictionary<string, List<Action<StockInput>>>(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> ConnectStreamApis()
        { 
            var dataTask = _alpacaDataStreamingClient.ConnectAndAuthenticateAsync();
            var tradingTask = _alpacaTradingStreamingClient.ConnectAndAuthenticateAsync();

            return (await dataTask) == AuthStatus.Authorized && (await tradingTask) == AuthStatus.Authorized;
        }

        public async Task DisconnectStreamApis()
        {
            await _alpacaDataStreamingClient.DisconnectAsync();
            await _alpacaTradingStreamingClient.DisconnectAsync();
        }

        public void AddPendingMinuteAggSubscription(string stockSymbol, Action<StockInput> action)
        {
            if (_stockActions.TryGetValue(stockSymbol, out var actionList))
            {
                actionList.Add(action);
            }
            else
            {
                _stockActions.Add(stockSymbol, new List<Action<StockInput>> { action });
            }
        }

        public void SubscribeToMinuteAgg()
        {
            var newClient = _alpacaDataStreamingClient.GetMinuteAggSubscription();
            newClient.Received += HandleMinuteAgg;
            _alpacaDataStreamingClient.Subscribe(newClient);
        }

        private void HandleMinuteAgg(IStreamAgg newValue) 
        {
            if (_stockActions.TryGetValue(newValue.Symbol, out var actionList))
            {
                actionList.ForEach(action =>
                    action(new StockInput
                    {
                        ClosingPrice = newValue.Close,
                        Time = newValue.EndTimeUtc,
                        StockSymbol = newValue.Symbol
                    })
                );
            }
        }

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
        /// <param name="lookBack">Number of units to look back</param>
        /// <returns>Tuple with training data and test data in that order</returns>
        public async Task<IList<StockInput>> GetStockData(string stockSymbol, TradingFrequency aggUnits, int lookBack = 1000)
        {
            var stockData = await _alpacaDataClient.GetBarSetAsync(new BarSetRequest(stockSymbol, (TimeFrame) aggUnits)
            {
                Limit = lookBack
            });

            return GetStockInputs(stockSymbol, stockData[stockSymbol]);
        }

        public async Task<IEnumerable<DateTime>> GetAllTradingHolidays(DateTime? start = null, DateTime? end = null)
        {
            var startValue = start ?? DateTime.UtcNow;
            var endValue = end ?? startValue.AddYears(10);

            var tradingDays = (await _alpacaTradingClient.ListCalendarAsync(new CalendarRequest().SetInclusiveTimeInterval(startValue, endValue))).Select(x => x.TradingDateUtc.Date);
            var allDays = Enumerable.Range(0, 1 + endValue.Subtract(startValue).Days)
                              .Select(offset => startValue.AddDays(offset).Date);

            return allDays.Except(tradingDays);
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
