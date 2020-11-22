using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stonks2.Configuration;
using Stonks2.Stratagies;
using Stonks2.Alpaca;
using System.Linq;
using Moq;
using System.Threading.Tasks;
using Stonks2.Stratagies.MLStrategy;
using Stonks2.Stratagies.MicrotrendStrategy;
using System.Diagnostics;
using Stonks2.Stratagies.MeanReversionStrategy;

namespace Stonks2.Tests.Stategies
{
    [TestClass, TestCategory("Large")]
    public class LargeStrategiesTests
    {
        private IAlpacaClient _alpacaClient;
        private Mock<IAlpacaClient> _mockAlpacaClient;
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
        }

        [TestCleanup]
        public void CleanUp()
            => _alpacaClient?.Dispose();

        [TestMethod]
        public async Task ShouldBuyStock_MeanReversionStrategy_MakesMoney()
        {
            var strategy = new MeanReversionStrategy(_mockAlpacaClient.Object);
            
            var totalMoneyMade = await TestStrategy(strategy);

            if (totalMoneyMade == 0) Assert.Inconclusive("No money lost or made");
            Assert.IsTrue(totalMoneyMade > 0);        
        }

        [TestMethod]
        public async Task ShouldBuyStock_MicrotrendStrategy_MakesMoney()
        {
            var strategy = new MicrotrendStrategy(_mockAlpacaClient.Object);

            var totalMoneyMade = await TestStrategy(strategy);

            if(totalMoneyMade == 0) Assert.Inconclusive("No money lost or made");
            Assert.IsTrue(totalMoneyMade > 0);
        }

        [TestMethod]
        public async Task ShouldBuyStock_MLStrategy_MakesMoney()
        {
            var strategy = new MLStrategy(_mockAlpacaClient.Object, _config.Get<MLConfig>());

            var totalMoneyMade = await TestStrategy(strategy);

            if (totalMoneyMade == 0) Assert.Inconclusive("No money lost or made");
            Assert.IsTrue(totalMoneyMade > 0);
        }

        private async Task<decimal> TestStrategy(Strategy strategy)
        {
            var totalMoneyMade = 0m;

            foreach (var stock in _config.Get<StockConfig>().Stock_List.Take(5))
            {
                await TestStrategyOnStock(strategy, stock);

                Debug.WriteLine($"Money made on {stock}: {strategy.MoneyTracker.MoneyMade}");

                totalMoneyMade += strategy.MoneyTracker.MoneyMade;
                strategy.MoneyTracker.MoneyMade = 0;
                strategy.MoneyTracker.CostOfLastPosition = 0;

                Debug.WriteLine($"Total so far: {totalMoneyMade}");
            }

            return totalMoneyMade;
        }

        private async Task TestStrategyOnStock(Strategy strategy, string stock)
        {
            var data = (await _alpacaClient.GetStockData(stock)).OrderByDescending(x => x.Time).ToList();

            var sizeOfTestSet = data.Count / 5;
            var testData = data.Take(sizeOfTestSet);
            strategy.HistoricalData = data.Skip(sizeOfTestSet).ToList();

            var ownTheStock = false;
            foreach (var min in testData)
            {
                bool? calledBuy = null;
                //Mock out client so if we want to buy or sell, we are buying or selling only one share
                _mockAlpacaClient
                    .Setup(x => x.EnsurePositionExists(min.StockSymbol, min.ClosingPrice))
                    .Callback(() => calledBuy = true)
                    .ReturnsAsync(ownTheStock ? 0 : min.ClosingPrice);
                _mockAlpacaClient
                    .Setup(x => x.EnsurePostionCleared(min.StockSymbol))
                    .Callback(() => calledBuy = false)
                    .ReturnsAsync(ownTheStock ? min.ClosingPrice : 0);

                await strategy.HandleMinuteAgg(min);

                ownTheStock = calledBuy ?? ownTheStock;
            }
        }
    }
}
