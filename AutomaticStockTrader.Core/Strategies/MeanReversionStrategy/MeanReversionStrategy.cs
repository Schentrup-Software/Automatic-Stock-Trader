using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutomaticStockTrader.Domain;

namespace AutomaticStockTrader.Core.Strategies.MeanReversionStrategy
{
    public class MeanReversionStrategy : IStrategy
    {
        public Task<bool?> ShouldBuyStock(IList<StockInput> historicalData)
        {
            if (historicalData.Count > 20)
            {
                var histData = historicalData.Take(20).ToList();

                var avg = histData.Select(x => x.ClosingPrice).Average();
                var diff = avg - histData.OrderByDescending(x => x.Time).First().ClosingPrice;

                return Task.FromResult<bool?>(diff >= 0);
            }
            else
            {
                return Task.FromResult<bool?>(null);
            }
        }
    }
}
