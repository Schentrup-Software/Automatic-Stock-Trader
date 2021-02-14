using AutomaticStockTrader.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Core.Strategies
{
    public interface IStrategy
    {
        Task<bool?> ShouldBuyStock(IList<StockInput> HistoricalData);
    }
}
