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

namespace AutomaticStockTrader.Tests.Stategies
{
    [TestClass, TestCategory("Large")]
    public class LargeStrategiesTests
    {
        private IAlpacaClient _alpacaClient;
        private Mock<IAlpacaClient> _mockAlpacaClient;
        private StockContext _context;
        private ITrackingRepository _repo;
        private IConfigurationRoot _config;

        [TestInitialize]
        public void SetUp()
        {
            LaunchSettingsFixture.SetupEnvVars();
            _config = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            _alpacaClient = new AlpacaClient(_config.Get<AlpacaConfig>());
            _mockAlpacaClient = new Mock<IAlpacaClient>();
            _mockAlpacaClient.Setup(x => x.GetTotalEquity()).ReturnsAsync(100_000m);

            _context = new StockContext();
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            _repo = new TrackingRepository(_context);
        }

        [TestCleanup]
        public void CleanUp()
        {
            _alpacaClient?.Dispose();
            _repo?.Dispose();
        }

        [TestMethod]
        public async Task ShouldBuyStock_MeanReversionStrategy_MakesMoney()
        {
            var strategy = new MeanReversionStrategy(_mockAlpacaClient.Object, _repo, TradingFrequency.Minute, 0.1m);
            
            var totalMoneyMade = await TestStrategy(strategy);

            if (totalMoneyMade == 0) Assert.Inconclusive("No money lost or made");
            Assert.IsTrue(totalMoneyMade > 0);        
        }

        [TestMethod]
        public async Task ShouldBuyStock_MicrotrendStrategy_MakesMoney()
        {
            var strategy = new MicrotrendStrategy(_mockAlpacaClient.Object, _repo, TradingFrequency.Minute, 0.1m);

            var totalMoneyMade = await TestStrategy(strategy);

            if(totalMoneyMade == 0) Assert.Inconclusive("No money lost or made");
            Assert.IsTrue(totalMoneyMade > 0);
        }

        [TestMethod]
        public async Task ShouldBuyStock_MLStrategy_MakesMoney()
        {
            var strategy = new MLStrategy(_mockAlpacaClient.Object, _repo, TradingFrequency.Minute, 0.1m, _config.Get<MLConfig>());

            var totalMoneyMade = await TestStrategy(strategy, true);

            if (totalMoneyMade == 0) Assert.Inconclusive("No money lost or made");
            Assert.IsTrue(totalMoneyMade > 0);
        }

        private async Task<decimal> TestStrategy(Strategy strategy, bool useHistoricalData = false)
        {
            var totalMoneyMade = 0m;

            foreach (var stock in _config.Get<StockConfig>().Stock_List.Take(5))
            {
                await TestStrategyOnStock(strategy, stock, useHistoricalData);

                var orders = _context.Orders
                    .Where(x => x.Position.StockSymbol == stock)
                    .Select(x => x.ActualSharesBought.Value * x.ActualCostPerShare.Value)
                    .AsEnumerable();

                var moneyMade = orders.Any() ? orders.Aggregate((x, y) => x + y) : 0;
                Debug.WriteLine($"Money made on {stock}: {moneyMade}");
             
                totalMoneyMade += moneyMade;
                Debug.WriteLine($"Total so far: {totalMoneyMade}");
            }

            return totalMoneyMade;
        }

        private async Task TestStrategyOnStock(Strategy strategy, string stock, bool useHistoricaData)
        {
            var data = (await _alpacaClient.GetStockData(stock)).OrderByDescending(x => x.Time).ToList();

            var sizeOfTestSet = useHistoricaData ? data.Count / 5 : data.Count;
            var testData = data.Take(sizeOfTestSet);
            strategy.HistoricalData = data.Skip(sizeOfTestSet).ToList();

            foreach (var min in testData)
            {
                _mockAlpacaClient
                    .Setup(x => x.PlaceOrder(It.Is<StrategysStock>(x => x.StockSymbol == min.StockSymbol), It.IsAny<Order>()))
                    .Callback<StrategysStock, Order>((s, o) => _repo.CompleteOrder(min.StockSymbol, min.ClosingPrice, o.SharesBought).Wait());

                await strategy.HandleMinuteAgg(min);
            }
        }
    }
}
