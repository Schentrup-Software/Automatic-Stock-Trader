using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Strategies;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Tests.Stategies
{
    [TestClass, TestCategory("Small")]
    public class StrategyHandlerTests
    {
        private Mock<ILogger<StrategyHandler>> _mockLogger;
        private Mock<IAlpacaClient> _mockAlpacaClient; 
        private Mock<ITrackingRepository> _mockTrackingRepository;
        private Mock<IStrategy> _mockStrategy;

        private const decimal _percentageOfEquityToAllocate = 0.01m;
        private const decimal _totalEquity = 100_000m;
        private const string _stockSymbol = "FooBar";
        private const string _strategyName = "BarFooStrat";
        private const TradingFrequency _tradingFrequency = TradingFrequency.Minute;
        private StrategyHandler _strategyHandler;

        [TestInitialize]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<StrategyHandler>>();
            _mockAlpacaClient = new Mock<IAlpacaClient>();
            _mockTrackingRepository = new Mock<ITrackingRepository>();
            _mockStrategy = new Mock<IStrategy>();

            _strategyHandler = new StrategyHandler(
                _mockLogger.Object,
                _mockAlpacaClient.Object,
                _mockTrackingRepository.Object,
                _mockStrategy.Object,
                _tradingFrequency,
                _percentageOfEquityToAllocate,
                _stockSymbol
            );
        }

        [TestMethod]
        [DataRow(1000, 1)]
        [DataRow(1, 1000)]
        [DataRow(5, 200)]
        [DataRow(1000, 0.91)]
        [DataRow(100, 9.9)]
        [DataRow(999, 1)]
        public async Task HandleBuy_NoNeedToBuy_DoesNotBuy(int shares, double price)
        {
            var strategysStock = new StrategysStock
            {
                StockSymbol = _stockSymbol,
                TradingFrequency = _tradingFrequency,
                Strategy = _strategyName
            };

            _mockAlpacaClient
                .Setup(x => x.GetTotalEquity())
                .ReturnsAsync(_totalEquity);
            _mockTrackingRepository
                .Setup(x => x.GetOrCreateEmptyPosition(strategysStock))
                .ReturnsAsync(new Position
                {
                    NumberOfShares = shares,
                    StockSymbol = _stockSymbol
                });

            await _strategyHandler.HandleBuy((decimal)price, strategysStock);

            _mockAlpacaClient.Verify(x => x.PlaceOrder(It.IsAny<Order>(), null), Times.Never);
            _mockTrackingRepository.Verify(x => x.AddPendingOrder(It.IsAny<StrategysStock>(), It.IsAny<Order>()), Times.Never);
        }

        [TestMethod]
        [DataRow(0, 1, 1000)]
        [DataRow(500, 1, 500)]
        [DataRow(9, 100, 1)]
        [DataRow(0, 100, 10)]
        [DataRow(1000, 0.90, 111)]
        public async Task HandleBuy_NeedsToBuy_BuysCorrectAmount(int shares, double price, int shareBought)
        {
            var strategysStock = new StrategysStock
            {
                StockSymbol = _stockSymbol,
                TradingFrequency = _tradingFrequency,
                Strategy = _strategyName
            };

            _mockAlpacaClient
                .Setup(x => x.GetTotalEquity())
                .ReturnsAsync(_totalEquity);
            _mockTrackingRepository
                .Setup(x => x.GetOrCreateEmptyPosition(strategysStock))
                .ReturnsAsync(new Position
                {
                    NumberOfShares = shares,
                    StockSymbol = _stockSymbol
                });

            await _strategyHandler.HandleBuy((decimal)price, strategysStock);

            _mockAlpacaClient.Verify(x => x.PlaceOrder(It.Is<Order>(x => x.SharesBought == shareBought), null), Times.Once);
            _mockTrackingRepository.Verify(x => x.AddPendingOrder(It.IsAny<StrategysStock>(), It.Is<Order>(x => x.SharesBought == shareBought)), Times.Once);
        }

        [TestMethod]
        public async Task HandleSell_NeedsToSell_SellsCorrectAmount()
        {
            var shares = 123;
            var price = 456m;

            var strategysStock = new StrategysStock
            {
                StockSymbol = _stockSymbol,
                TradingFrequency = _tradingFrequency,
                Strategy = _strategyName
            };

            _mockAlpacaClient
                .Setup(x => x.GetTotalEquity())
                .ReturnsAsync(_totalEquity);
            _mockTrackingRepository
                .Setup(x => x.GetOrCreateEmptyPosition(strategysStock))
                .ReturnsAsync(new Position
                {
                    NumberOfShares = shares,
                    StockSymbol = _stockSymbol
                });

            await _strategyHandler.HandleSell(price, strategysStock);

            _mockAlpacaClient.Verify(x => x.PlaceOrder(It.Is<Order>(x => x.SharesBought == shares * (-1)), null), Times.Once);
            _mockTrackingRepository.Verify(x => x.AddPendingOrder(It.IsAny<StrategysStock>(), It.Is<Order>(x => x.SharesBought == shares * (-1))), Times.Once);
        }

        [TestMethod]
        public async Task HandleSell_NoNeedToBuy_DoesNotSell()
        {
            var shares = 0;
            var price = 456m;

            var strategysStock = new StrategysStock
            {
                StockSymbol = _stockSymbol,
                TradingFrequency = _tradingFrequency,
                Strategy = _strategyName
            };

            _mockAlpacaClient
                .Setup(x => x.GetTotalEquity())
                .ReturnsAsync(_totalEquity);
            _mockTrackingRepository
                .Setup(x => x.GetOrCreateEmptyPosition(strategysStock))
                .ReturnsAsync(new Position
                {
                    NumberOfShares = shares,
                    StockSymbol = _stockSymbol
                });

            await _strategyHandler.HandleSell(price, strategysStock);

            _mockAlpacaClient.Verify(x => x.PlaceOrder(It.IsAny<Order>(), null), Times.Never);
            _mockTrackingRepository.Verify(x => x.AddPendingOrder(It.IsAny<StrategysStock>(), It.IsAny<Order>()), Times.Never);
        }
    }
}
