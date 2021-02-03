using AutomaticStockTrader.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Core.Strategies
{
    public interface IStrategy
    {
        Task<bool?> ShouldBuyStock(IList<StockInput> HistoricalData);
    }
}
