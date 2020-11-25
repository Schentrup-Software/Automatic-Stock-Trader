using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutomaticStockTrader.Alpaca;
using AutomaticStockTrader.Configuration;

namespace AutomaticStockTrader.Stratagies.MLStrategy
{
    public class MLStrategy : Strategy
    {
        private readonly MLConfig _config;

        public MLStrategy(IAlpacaClient client, MLConfig config) : base(client)
        {
            _config = config;
        }

        public override Task<bool?> ShouldBuyStock(StockInput newData)
        {
            HistoricalData.Add(newData);
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
