using AutomaticStockTrader.Core.Strategies.MicrotrendStrategy;
using AutomaticStockTrader.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Tests.Stategies
{
    [TestClass, TestCategory("Small")]
    public class MicrotrendStrategyTests
    {
        private MicrotrendStrategy _strategy;

        [TestInitialize]
        public void SetUp()
        {
            _strategy = new MicrotrendStrategy();
        }

        [TestMethod]
        public async Task ShouldBuyStock_TrendingUp_TakesPosition()
        {
            var now = DateTime.Now;
            var historicalData = new List<StockInput>()
            {
                new StockInput {ClosingPrice = 10, Time = now.AddMinutes(-1)},
                new StockInput {ClosingPrice = 11, Time = now.AddMinutes(-3)},
                new StockInput {ClosingPrice = 9, Time = now.AddMinutes(-2)},
                new StockInput { ClosingPrice = 11, Time = now },
            }
            .OrderByDescending(x => x.Time)
            .ToList();

            var result = await _strategy.ShouldBuyStock(historicalData);

            Assert.IsTrue(result.Value);
        }

        [TestMethod]
        public async Task ShouldBuyStock_NoTrends_HoldPosition()
        {
            var now = DateTime.Now;
            var historicalData = new List<StockInput>()
            {
                new StockInput {ClosingPrice = 10, Time = now.AddMinutes(-1)},
                new StockInput {ClosingPrice = 11, Time = now.AddMinutes(-3)},
                new StockInput {ClosingPrice = 9, Time = now.AddMinutes(-2)},
                new StockInput { ClosingPrice = 10, Time = now },
            }
            .OrderByDescending(x => x.Time)
            .ToList();

            var result = await _strategy.ShouldBuyStock(historicalData);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task ShouldBuyStock_NotEnoughData_HoldPosition()
        {
            var now = DateTime.Now;
            var historicalData = new List<StockInput>()
            {
                new StockInput {ClosingPrice = 1, Time = now.AddMinutes(-1)},
                new StockInput { ClosingPrice = 10, Time = now },
            }
            .OrderByDescending(x => x.Time)
            .ToList();

            var result = await _strategy.ShouldBuyStock(historicalData);

            Assert.IsNull(result);
        }

        [TestMethod]
        [DataRow(1, 2, 3, 2)]
        [DataRow(1, 3, 2, 3)]
        [DataRow(1, 4, 3, 2)]
        public async Task ShouldBuyStock_TrendDown_SellPosition(double value1, double value2, double value3, double value4)
        {
            var now = DateTime.Now;
            var historicalData = new List<StockInput>()
            {
                new StockInput {ClosingPrice = (decimal)value3, Time = now.AddMinutes(-1)},
                new StockInput {ClosingPrice = (decimal)value1, Time = now.AddMinutes(-3)},
                new StockInput {ClosingPrice = (decimal)value2, Time = now.AddMinutes(-2)},
                new StockInput { ClosingPrice = (decimal)value4, Time = now },
            }
            .OrderByDescending(x => x.Time)
            .ToList();

            var result = await _strategy.ShouldBuyStock(historicalData);

            Assert.IsFalse(result.Value);
        }
    }
}
