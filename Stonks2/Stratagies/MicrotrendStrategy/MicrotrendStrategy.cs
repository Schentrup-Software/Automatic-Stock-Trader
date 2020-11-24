using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stonks2.Alpaca;

namespace Stonks2.Stratagies.MicrotrendStrategy
{
    public class MicrotrendStrategy : Strategy
    {
        public MicrotrendStrategy(IAlpacaClient client) : base(client) { }

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
