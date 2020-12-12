using System.Linq;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository;

namespace AutomaticStockTrader.Core.Strategies.MLStrategy
{
    public class MLStrategy : Strategy
    {
        private readonly MLConfig _config;

        public MLStrategy(IAlpacaClient alpacaClient, ITrackingRepository trackingRepository, TradingFrequency tradingFrequency, decimal percentageOfEquityToAllocate, MLConfig config) 
            : base(alpacaClient, trackingRepository, tradingFrequency, percentageOfEquityToAllocate)
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
