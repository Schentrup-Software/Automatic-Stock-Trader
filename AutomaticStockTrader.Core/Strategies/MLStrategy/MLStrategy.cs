using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Domain;

namespace AutomaticStockTrader.Core.Strategies.MLStrategy
{
    public class MLStrategy : IStrategy
    {
        private readonly MLConfig _config;

        public MLStrategy(MLConfig config) 
        {
            _config = config;
        }

        public Task<bool?> ShouldBuyStock(IList<StockInput> HistoricalData)
        {
            var modelBuilder = new ModelBuilder(_config);
            var model = modelBuilder.BuildModel(HistoricalData.Select(x => new ModelInput
            {
                PriceDiffrence = (float)((x.ClosingPrice - HistoricalData.Last().ClosingPrice) / HistoricalData.Last().ClosingPrice),
                Time = x.Time
            }).ToList());
            var result = model.Predict();

            return Task.FromResult((bool?) (result.ForecastedPriceDiffrence[0] > 0));
        }
    }
}
