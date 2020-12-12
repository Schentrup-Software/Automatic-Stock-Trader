using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Strategies.MeanReversionStrategy;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Tests.Stategies
{
    [TestClass, TestCategory("Small")]
    public class MeanReversionStrategyTests
    {
        private MeanReversionStrategy _strategy;
        private IList<StockInput> _histoicData;

        [TestInitialize]
        public void SetUp()
        {
            _strategy = new MeanReversionStrategy(new Mock<IAlpacaClient>().Object, new Mock<ITrackingRepository>().Object, TradingFrequency.Minute, 0.1m);
            _histoicData = Enumerable.Range(1, 20).Select(x => new StockInput
            {
                ClosingPrice = x % 3,
                Time = DateTime.Now.AddMinutes(-1 * x)
            }).ToList();
        }

        [TestMethod]
        public async Task ShouldBuyStock_BelowAverage_TakesPosition()
        {
            var now = DateTime.Now;
            _strategy.HistoricalData = _histoicData;

            var result = await _strategy.ShouldBuyStock(new StockInput { ClosingPrice = 0.5m, Time = now });

            Assert.IsTrue(result.Value);
        }

        [TestMethod]
        public async Task ShouldBuyStock_NotEnoughData_HoldPosition()
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
        public async Task ShouldBuyStock_AboveAverage_SellPosition()
        {
            var now = DateTime.Now;
            _strategy.HistoricalData = _histoicData;

            var result = await _strategy.ShouldBuyStock(new StockInput { ClosingPrice = 4, Time = now });

            Assert.IsFalse(result.Value);
        }
    }
}
