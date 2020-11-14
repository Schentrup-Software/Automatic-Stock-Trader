using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alpaca.Markets;
using Stonks2.Configuration;

namespace Stonks2
{
    public class AlpacaClient : IDisposable
    {
        private readonly AlpacaConfig _config;
        private readonly IAlpacaTradingClient _alpacaTradingClient;
        private readonly IPolygonDataClient _polygonDataClient;
        private readonly IPolygonStreamingClient _polygonStreamingClient;
        
        private bool disposedValue;

        public AlpacaClient(AlpacaConfig config)
        {
            _config = config;
            _alpacaTradingClient = config.Run_In_Production
                ? Environments.Live.GetAlpacaTradingClient(new SecretKey(_config.Live_Key_Id, _config.Secret_Key))
                : Environments.Paper.GetAlpacaTradingClient(new SecretKey(_config.Paper_Key_Id, _config.Secret_Key));
            _polygonDataClient = Environments.Live.GetPolygonDataClient(_config.Live_Key_Id);
            _polygonStreamingClient = Environments.Live.GetPolygonStreamingClient(_config.Live_Key_Id);
        }

        public async Task<bool> ConnectStreamApi()
            => (await _polygonStreamingClient.ConnectAndAuthenticateAsync()) == AuthStatus.Authorized;

        public async Task SubscribeToQuoteChange(string stockSymbol, Action<IStreamAgg> action)
        {
            _polygonStreamingClient.SubscribeQuote(stockSymbol);
            _polygonStreamingClient.MinuteAggReceived += action;
        }

        public async Task TakePosition(string stockSymbol)
        {
            var postionTask = _alpacaTradingClient.GetPositionAsync(stockSymbol);
            var currentAccountTask = _alpacaTradingClient.GetAccountAsync();

            var targetEquityAmount = (await currentAccountTask).Equity * _config.Percentage_Of_Equity_Per_Position;
            var marketValue = (await postionTask)?.MarketValue ?? 0;

            if (marketValue < targetEquityAmount) 
            {
                var missingEquity = targetEquityAmount - marketValue;
                var numberOfSharesNeeded = (int) Math.Floor(missingEquity / marketValue);
                await _alpacaTradingClient.PostOrderAsync(new NewOrderRequest(stockSymbol, numberOfSharesNeeded, OrderSide.Buy, OrderType.Market, TimeInForce.Ioc));
            }
        }

        public async Task ClearPostion(string stockSymbol)
        {
            var postion = await _alpacaTradingClient.GetPositionAsync(stockSymbol);

            if ((postion?.MarketValue ?? 0) != 0)
            {
                await _alpacaTradingClient.PostOrderAsync(new NewOrderRequest(stockSymbol, postion.Quantity, OrderSide.Sell, OrderType.Market, TimeInForce.Ioc));
            }
        }

        /// <summary>
        /// Get stock data for single symbol
        /// </summary>
        /// <param name="stockSymbol">Stock symbol to get data for</param>
        /// <returns>Tuple with training data and test data in that order</returns>
        public async Task<IList<ModelInput>> GetStockData(string stockSymbol)
        {
            var stockData = await _polygonDataClient.ListAggregatesAsync(
                new AggregatesRequest(stockSymbol, new AggregationPeriod(1, (AggregationPeriodUnit)_config.Aggregation_Period_Unit))
                .SetInclusiveTimeInterval(DateTime.Now.AddDays(-1 * _config.Days_To_Look_Back), DateTime.Now));

            var trainData = GetModelInputs(stockData.Items.ToList());

            return trainData;
        }

        private static IList<ModelInput> GetModelInputs(IList<IAgg> aggs) => aggs
            .Select((x, i) => new ModelInput
            {
                PriceDiffrence = (float)(i == 0 ? 0 : (x.Close - aggs[i - 1].Close) / aggs[i - 1].Close),
                Time = x.TimeUtc ?? throw new ArgumentNullException(nameof(x.TimeUtc)),
                ClosingPrice = x.Close,
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
                    _polygonStreamingClient.Dispose();
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
