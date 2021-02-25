using AutomaticStockTrader.Repository;
using AutomaticStockTrader.Repository.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Tests
{
    [TestClass, TestCategory("Small")]
    public class TrackingRepositoryTests
    {
        private StockContext _context;
        private TrackingRepository _repo;
        private InitStockContext _initStockContext;

        private const string _stockSymbol = "Foobar";
        private const string _strategy = "FooStrategy";
        private const Domain.TradingFrequency _tradingFrequency = Domain.TradingFrequency.Week;
        private readonly static Guid _positionId = Guid.Parse("c99e1eff-a4c1-479c-874a-cc6faf091c91");

        [TestInitialize]
        public void SetUp()
        {
            _context = new StockContext();
            _initStockContext = new InitStockContext(Mock.Of<ILogger<InitStockContext>>(), _context);
            _initStockContext.StartAsync(default).Wait();

            _repo = new TrackingRepository(_context);
        }

        [TestCleanup]
        public void CleanUp()
        {
            _initStockContext.StopAsync(default).Wait();
            _repo?.Dispose();
        }

        [TestMethod]
        public async Task GetOrCreateEmptyPosition_WithPositionAndOrders_ReturnsCorrectAmount()
        {
            _context.StratagysStocks.Add(GetStratagysStock());
            _context.Orders.AddRange(new List<Order>
            {
                GetOrder(1, 10),
                GetOrder(-1, 11),
                GetOrder(1, 10),
                GetOrder(1, 11),
                GetOrder(-2, 11),
                GetOrder(12, 11),
            });
            await _context.SaveChangesAsync();

            var result = await _repo.GetOrCreateEmptyPosition(GetDomainStrategysStock());

            Assert.AreEqual(_stockSymbol, result.StockSymbol);
            Assert.AreEqual(12, result.NumberOfShares);
        }

        [TestMethod]
        public async Task GetOrCreateEmptyPosition_WithPositionAndNoOrders_ReturnsNone()
        {
            _context.StratagysStocks.Add(GetStratagysStock());
            await _context.SaveChangesAsync();

            var result = await _repo.GetOrCreateEmptyPosition(GetDomainStrategysStock());

            Assert.AreEqual(_stockSymbol, result.StockSymbol);
            Assert.AreEqual(0, result.NumberOfShares);
        }

        [TestMethod]
        public async Task GetOrCreateEmptyPosition_NoPositionAndNoOrders_ReturnsNoneAndCreates()
        {
            var result = await _repo.GetOrCreateEmptyPosition(GetDomainStrategysStock());

            Assert.AreEqual(_stockSymbol, result.StockSymbol);
            Assert.AreEqual(0, result.NumberOfShares);
            Assert.IsTrue(_context.StratagysStocks.Any(x => x.StockSymbol == _stockSymbol && x.Strategy == _strategy && x.TradingFrequency == _tradingFrequency));
        }

        private static StratagysStock GetStratagysStock()
            => new StratagysStock
            {
                Id = _positionId,
                StockSymbol = _stockSymbol,
                Strategy = _strategy,
                TradingFrequency = _tradingFrequency
            };

        private static Domain.StrategysStock GetDomainStrategysStock()
            => new Domain.StrategysStock
            {
                StockSymbol = _stockSymbol,
                Strategy = _strategy,
                TradingFrequency = _tradingFrequency
            };

        private static Order GetOrder(long actualSharesBought, decimal actualCostPerShare)
            => new Order
            {
                Id = Guid.NewGuid(),
                ActualCostPerShare = actualCostPerShare,
                ActualSharesBought = actualSharesBought,
                AttemptedCostPerShare = 1m,
                AttemptedSharesBought = 2,
                OrderPlaced = DateTime.Now,
                PositionId = _positionId
            };
    }
}
