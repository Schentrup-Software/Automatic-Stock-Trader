using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Stonks2.Alpaca;
using Stonks2.Stratagies.MicrotrendStrategy;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stonks2.Tests.Stategies
{
    [TestClass, TestCategory("Small")]
    public class MicrotrendStrategyTests
    {
        private MicrotrendStrategy _strategy;

        [TestInitialize]
        public void SetUp()
        {
            _strategy = new MicrotrendStrategy(new Mock<IAlpacaClient>().Object);
        }

        [TestMethod]
        public async Task ShouldBuyStock_TrendingUp_TakesPosition()
        {
            var now = DateTime.Now;
            _strategy.HistoricalData = new List<StockInput>()
            {
                new StockInput {ClosingPrice = 10, Time = now.AddMinutes(-1)},
                new StockInput {ClosingPrice = 11, Time = now.AddMinutes(-3)},
                new StockInput {ClosingPrice = 9, Time = now.AddMinutes(-2)},
            };

            var result = await _strategy.ShouldBuyStock(new StockInput { ClosingPrice = 11, Time = now });

            Assert.IsTrue(result.Value);
        }

        [TestMethod]
        public async Task ShouldBuyStock_NoTrends_HoldPosition()
        {
            var now = DateTime.Now;
            _strategy.HistoricalData = new List<StockInput>()
            {
                new StockInput {ClosingPrice = 10, Time = now.AddMinutes(-1)},
                new StockInput {ClosingPrice = 11, Time = now.AddMinutes(-3)},
                new StockInput {ClosingPrice = 9, Time = now.AddMinutes(-2)},
            };

            var result = await _strategy.ShouldBuyStock(new StockInput { ClosingPrice = 10, Time = now });

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task ShouldBuyStock_NotEnoughData_HoldPosition()
        {
            var now = DateTime.Now;
            _strategy.HistoricalData = new List<StockInput>()
            {
                new StockInput {ClosingPrice = 1, Time = now.AddMinutes(-1)},
            };

            var result = await _strategy.ShouldBuyStock(new StockInput { ClosingPrice = 10, Time = now });

            Assert.IsNull(result);
        }

        [TestMethod]
        [DataRow(1, 2, 3, 2)]
        [DataRow(1, 3, 2, 3)]
        [DataRow(1, 4, 3, 2)]
        public async Task ShouldBuyStock_TrendDown_SellPosition(double value1, double value2, double value3, double value4)
        {
            var now = DateTime.Now;
            _strategy.HistoricalData = new List<StockInput>()
            {
                new StockInput {ClosingPrice = (decimal)value3, Time = now.AddMinutes(-1)},
                new StockInput {ClosingPrice = (decimal)value1, Time = now.AddMinutes(-3)},
                new StockInput {ClosingPrice = (decimal)value2, Time = now.AddMinutes(-2)},
            };

            var result = await _strategy.ShouldBuyStock(new StockInput { ClosingPrice = (decimal)value4, Time = now });

            Assert.IsFalse(result.Value);
        }
    }
}
