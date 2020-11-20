using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpaca.Markets;
using Stonks2.Configuration;

namespace Stonks2.Alpaca
{
    public class AlpacaClient : IAlpacaClient
    {
        private readonly AlpacaConfig _config;
        private readonly IAlpacaTradingClient _alpacaTradingClient;
        private readonly IPolygonDataClient _polygonDataClient;
        private readonly IDictionary<string, IPolygonStreamingClient> _polygonStreamingClients;
        
        private bool disposedValue;

        public AlpacaClient(AlpacaConfig config)
        {
            _config = config;
            _alpacaTradingClient = config.Run_In_Production
                ? Environments.Live.GetAlpacaTradingClient(new SecretKey(_config.Live_Key_Id, _config.Secret_Key))
                : Environments.Paper.GetAlpacaTradingClient(new SecretKey(_config.Paper_Key_Id, _config.Secret_Key));
            _polygonDataClient = Environments.Live.GetPolygonDataClient(_config.Live_Key_Id);
            _polygonStreamingClients = new Dictionary<string, IPolygonStreamingClient>();
        }

        public async Task<bool> ConnectStreamApi(string stockSymbol)
        {
            if (_polygonStreamingClients.TryGetValue(stockSymbol, out var client))
            {
                return (await client.ConnectAndAuthenticateAsync()) == AuthStatus.Authorized;
            }
            else
            {
                var newClient = Environments.Live.GetPolygonStreamingClient(_config.Live_Key_Id);
                _polygonStreamingClients.Add(stockSymbol, newClient);
                return (await newClient.ConnectAndAuthenticateAsync()) == AuthStatus.Authorized;
            }
        }

        public void SubscribeMinuteAgg(string stockSymbol, Action<StockInput> action)
        {
            void convertedAction(IStreamAgg newValue) => action(new StockInput
            {
                ClosingPrice = newValue.Close,
                Time = newValue.EndTimeUtc,
                StockSymbol = stockSymbol
            });

            _polygonStreamingClients[stockSymbol].MinuteAggReceived += convertedAction;
            _polygonStreamingClients[stockSymbol].SubscribeMinuteAgg(stockSymbol);
        }

        /// <summary>
        /// Ensure a postion of the amount exists
        /// </summary>
        /// <param name="marketValue"></param>
        /// <returns>Cost of shares bought</returns>
        public async Task<decimal> EnsurePositionExists(string stockSymbol, decimal marketValue)
        {
            var postionTask = _alpacaTradingClient.ListPositionsAsync();
            var currentAccountTask = _alpacaTradingClient.GetAccountAsync();

            var targetEquityAmount = (await currentAccountTask).Equity * _config.Percentage_Of_Equity_Per_Position;
            var currentPositionsMarketValue = (await postionTask)?
                .SingleOrDefault(x => string.Equals(x.Symbol, stockSymbol, StringComparison.OrdinalIgnoreCase))?.MarketValue ?? 0;
            var missingEquity = targetEquityAmount - currentPositionsMarketValue;
            var numberOfSharesNeeded = (int)Math.Floor(missingEquity / marketValue);

            if (numberOfSharesNeeded > 0) 
            {
                await _alpacaTradingClient.PostOrderAsync(new NewOrderRequest(stockSymbol, numberOfSharesNeeded, OrderSide.Buy, OrderType.Market, TimeInForce.Ioc));
                return numberOfSharesNeeded * marketValue;
            }

            return 0;
        }

        /// <summary>
        /// Clear out any postion that exists
        /// </summary>
        /// <returns>Amount of money received from sale</returns>
        public async Task<decimal> EnsurePostionCleared(string stockSymbol)
        {
            var postion = (await _alpacaTradingClient.ListPositionsAsync()).SingleOrDefault(x => string.Equals(x.Symbol, stockSymbol, StringComparison.OrdinalIgnoreCase));
            
            if (postion != null)
            {
                await _alpacaTradingClient.PostOrderAsync(new NewOrderRequest(stockSymbol, postion.Quantity, OrderSide.Sell, OrderType.Market, TimeInForce.Ioc));
                return postion.MarketValue;
            }

            return 0;
        }

        /// <summary>
        /// Get stock data for single symbol
        /// </summary>
        /// <param name="stockSymbol">Stock symbol to get data for</param>
        /// <returns>Tuple with training data and test data in that order</returns>
        public async Task<IList<StockInput>> GetStockData(string stockSymbol)
        {
            var stockData = await _polygonDataClient.ListAggregatesAsync(
                new AggregatesRequest(stockSymbol, new AggregationPeriod(1, (AggregationPeriodUnit)_config.Aggregation_Period_Unit))
                .SetInclusiveTimeInterval(DateTime.Now.AddDays(-1 * _config.Days_To_Look_Back), DateTime.Now));

            var trainData = GetStockInputs(stockSymbol, stockData.Items);

            return trainData;
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
                    _polygonDataClient.Dispose();
                    _alpacaTradingClient.Dispose();
                    _polygonStreamingClients.ToList().ForEach(x => x.Value.Dispose());
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
