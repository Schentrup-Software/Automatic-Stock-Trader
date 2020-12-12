using System.Linq;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository;

namespace AutomaticStockTrader.Core.Strategies.MicrotrendStrategy
{
    public class MicrotrendStrategy : Strategy
    {
        public MicrotrendStrategy(IAlpacaClient alpacaClient, ITrackingRepository trackingRepository, TradingFrequency tradingFrequency, decimal percentageOfEquityToAllocate) 
            : base(alpacaClient, trackingRepository, tradingFrequency, percentageOfEquityToAllocate) { }

        public override Task<bool?> ShouldBuyStock(StockInput newData)
        {
            HistoricalData.Add(newData);
            HistoricalData = HistoricalData.OrderByDescending(x => x.Time).Take(3).ToList();

            var last3Values = HistoricalData.Select(x => x.ClosingPrice).ToList();

            //Default to hold
            var result = (bool?) null;
            
            if (last3Values.Count >= 3 && last3Values[0] > last3Values[1] && last3Values[1] > last3Values[2])
            {
                //Buy if we have 2 mins of increase
                result = true;
            } 
            else if (last3Values.Count >= 3 && (last3Values[0] < last3Values[1] || last3Values[1] < last3Values[2]))
            {
                //Sell if any decrease in price
                result = false;
            }

            return Task.FromResult(result);
        }
    }
}
