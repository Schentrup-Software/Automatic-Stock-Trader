using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Moq;
using System.Threading.Tasks;
using System.Diagnostics;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Core.Strategies.MLStrategy;
using AutomaticStockTrader.Core.Strategies.MeanReversionStrategy;
using AutomaticStockTrader.Core.Strategies.MicrotrendStrategy;
using AutomaticStockTrader.Repository;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository.Models;
using Order = AutomaticStockTrader.Domain.Order;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Alpaca.Markets;
using AutomaticStockTrader.Tests.Tracking;
using System;

namespace AutomaticStockTrader.Tests.Stategies
{
    [TestClass, TestCategory("Large")]
    public class LargeStrategiesTests
    {
        private IAlpacaClient _alpacaClient;
        private Mock<IAlpacaClient> _mockAlpacaClient;
        private StockContext _context;
        private InitStockContext _initStockContext;
        private ITrackingRepository _repo;
        private IConfigurationRoot _config;

        private TrackingConfig _trackingConfig;

        private const decimal TOTAL_EQUITY = 100_000m;

        [TestInitialize]
        public void SetUp()
        {
            LaunchSettingsFixture.SetupEnvVars();
            _config = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            _trackingConfig = _config.Get<TrackingConfig>();

            var alpacaConfig = _config.Get<AlpacaConfig>();
            var env = alpacaConfig.Alpaca_Use_Live_Api ? Environments.Live : Environments.Paper;
            var key = new SecretKey(alpacaConfig.Alpaca_App_Id, alpacaConfig.Alpaca_Secret_Key);

            _alpacaClient = new AlpacaClient(
                env.GetAlpacaTradingClient(key), 
                env.GetAlpacaStreamingClient(key), 
                env.GetAlpacaDataClient(key), 
                env.GetAlpacaDataStreamingClient(key)
            );

            _mockAlpacaClient = new Mock<IAlpacaClient>();
            _mockAlpacaClient.Setup(x => x.GetTotalEquity()).ReturnsAsync(TOTAL_EQUITY);

            _context = new StockContext();
            _initStockContext = new InitStockContext(Mock.Of<ILogger<InitStockContext>>(), _context);
            _initStockContext.StartAsync(default).Wait();

            _repo = new TrackingRepository(_context);
        }

        [TestCleanup]
        public void CleanUp()
        {
            _initStockContext.StopAsync(default).Wait();
            _alpacaClient?.Dispose();
            _repo?.Dispose();
        }

        [TestMethod]
        public async Task ShouldBuyStock_MeanReversionStrategy_MakesMoney()
        {
            var strategy = new MeanReversionStrategy();
            
            var totalMoneyMade = await TestStrategy(strategy);

            await TrackingRecordWriter.WriteData(_trackingConfig, (double)totalMoneyMade, strategy.GetType().Name);

            if (totalMoneyMade == 0) Assert.Inconclusive("No money lost or made");
            Assert.IsTrue(totalMoneyMade > 0);        
        }

        [TestMethod]
        public async Task ShouldBuyStock_MicrotrendStrategy_MakesMoney()
        {
            var strategy = new MicrotrendStrategy();

            var totalMoneyMade = await TestStrategy(strategy);

            await TrackingRecordWriter.WriteData(_trackingConfig, (double)totalMoneyMade, strategy.GetType().Name);

            if (totalMoneyMade == 0) Assert.Inconclusive("No money lost or made");
            Assert.IsTrue(totalMoneyMade > 0);
        }

        [TestMethod]
        public async Task ShouldBuyStock_MLStrategy_MakesMoney()
        {
            var strategy = new MLStrategy(_config.Get<MLConfig>());

            var totalMoneyMade = await TestStrategy(strategy, true);

            await TrackingRecordWriter.WriteData(_trackingConfig, (double)totalMoneyMade, strategy.GetType().Name);

            if (totalMoneyMade == 0) Assert.Inconclusive("No money lost or made");
            Assert.IsTrue(totalMoneyMade > 0);
        }

        private async Task<decimal> TestStrategy(IStrategy strategy, bool useHistoricalData = false)
        {
            var totalMoneyMade = 0m;

            foreach (var stock in _config.Get<StrategyConfig>().Stock_List_Parsed)
            {
                var strategyHandler = new StrategyHandler(Mock.Of<ILogger<StrategyHandler>>(), _mockAlpacaClient.Object, _repo, strategy, TradingFrequency.Minute, 0.1m, stock);

                var closingPrice = await TestStrategyOnStock(strategyHandler, stock, useHistoricalData);

                var orders = _context.Orders
                    .AsQueryable()
                    .Where(x => x.Position.StockSymbol == stock)
                    .Select(x => new { quantity = x.ActualSharesBought.Value, price = x.ActualCostPerShare.Value })
                    .ToList();

                var leftoverStockSale = orders.Any() ? orders.Select(x => x.quantity).Aggregate((x, y) => x + y) * closingPrice : 0;
                var amountMadeSoFar = orders.Any() ? orders.Select(x => x.quantity * x.price).Aggregate((x, y) => x + y) : 0;

                var moneyMade = ((amountMadeSoFar * (-1) + leftoverStockSale) / TOTAL_EQUITY) * 100;

                Debug.WriteLine($"Money made on {stock}: {moneyMade}%");
             
                totalMoneyMade += moneyMade;
                Debug.WriteLine($"Total so far: {totalMoneyMade}%");
            }

            return totalMoneyMade;
        }

        private async Task<decimal> TestStrategyOnStock(StrategyHandler strategy, string stock, bool useHistoricaData)
        {
            var data = (await _alpacaClient.GetStockData(stock, TradingFrequency.Minute, 500))
                .OrderBy(x => x.Time)
                .ToList();

            var sizeOfTestSet = useHistoricaData ? data.Count / 5 : data.Count;
            var testData = data.Take(sizeOfTestSet);
            
            strategy.HistoricalData.Clear();
            strategy.HistoricalData.AddRange(data.Skip(sizeOfTestSet).ToList());

            var lastPrice = 0m;
            foreach (var min in testData)
            {
                _mockAlpacaClient
                    .Setup(x => x.PlaceOrder(It.Is<Order>(x => x.StockSymbol == min.StockSymbol), null))
                    .Callback<StrategysStock, Order>((s, o) =>
                        _repo.CompleteOrder(new Order
                        {
                            StockSymbol = min.StockSymbol,
                            MarketPrice = o.MarketPrice,
                            SharesBought = o.SharesBought
                        }).Wait());

                await strategy.HandleNewData(min);
                lastPrice = min.ClosingPrice;
            }

            return lastPrice;
        }
    }
}
