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
        private readonly IAlpacaDataClient _alpacaDataClient;
        private readonly IAlpacaDataStreamingClient _alpacaStreamingClient;
        
        private bool disposedValue;

        public AlpacaClient(AlpacaConfig config)
        {
            _config = config;
            _alpacaTradingClient = config.Alpaca_Use_Live_Api
                ? Environments.Live.GetAlpacaTradingClient(new SecretKey(_config.Live_Key_Id, _config.Alpaca_Secret_Key))
                : Environments.Paper.GetAlpacaTradingClient(new SecretKey(_config.Alpaca_Key_Id, _config.Alpaca_Secret_Key));
            _alpacaDataClient = config.Alpaca_Use_Live_Api
                ? Environments.Live.GetAlpacaDataClient(new SecretKey(_config.Live_Key_Id, _config.Alpaca_Secret_Key))
                : Environments.Paper.GetAlpacaDataClient(new SecretKey(_config.Alpaca_Key_Id, _config.Alpaca_Secret_Key));
            _alpacaStreamingClient = config.Alpaca_Use_Live_Api
                ? Environments.Live.GetAlpacaDataStreamingClient(new SecretKey(_config.Live_Key_Id, _config.Alpaca_Secret_Key))
                : Environments.Paper.GetAlpacaDataStreamingClient(new SecretKey(_config.Alpaca_Key_Id, _config.Alpaca_Secret_Key));
        }

        public async Task<bool> ConnectStreamApi()
            => (await _alpacaStreamingClient.ConnectAndAuthenticateAsync()) == AuthStatus.Authorized;

        public void SubscribeMinuteAgg(string stockSymbol, Action<StockInput> action)
        {
            void convertedAction(IStreamAgg newValue) => action(new StockInput
            {
                ClosingPrice = newValue.Close,
                Time = newValue.EndTimeUtc,
                StockSymbol = stockSymbol
            });

            var newClient = _alpacaStreamingClient.GetMinuteAggSubscription(stockSymbol);
            newClient.Received += convertedAction;
            _alpacaStreamingClient.Subscribe(newClient);
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
                    _alpacaStreamingClient?.Dispose();
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
