using AutomaticStockTrader.Core.Strategies.MeanReversionStrategy;
using AutomaticStockTrader.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            _strategy = new MeanReversionStrategy();
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
            _histoicData.Add(new StockInput { ClosingPrice = 0.5m, Time = now });
            _histoicData = _histoicData
                .OrderByDescending(x => x.Time)
                .ToList();

            var result = await _strategy.ShouldBuyStock(_histoicData);

            Assert.IsTrue(result.Value);
        }

        [TestMethod]
        public async Task ShouldBuyStock_NotEnoughData_HoldPosition()
        {
            var now = DateTime.Now;
            _histoicData = new List<StockInput>()
            {
                new StockInput {ClosingPrice = 10, Time = now.AddMinutes(-1)},
                new StockInput {ClosingPrice = 11, Time = now.AddMinutes(-3)},
                new StockInput {ClosingPrice = 9, Time = now.AddMinutes(-2)},
                new StockInput { ClosingPrice = 10, Time = now },
            }
            .OrderByDescending(x => x.Time)
            .ToList();

            var result = await _strategy.ShouldBuyStock(_histoicData);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task ShouldBuyStock_AboveAverage_SellPosition()
        {
            var now = DateTime.Now;
            _histoicData.Add(new StockInput { ClosingPrice = 4, Time = now });
            _histoicData = _histoicData
                .OrderByDescending(x => x.Time)
                .ToList();

            var result = await _strategy.ShouldBuyStock(_histoicData);

            Assert.IsFalse(result.Value);
        }
    }
}
