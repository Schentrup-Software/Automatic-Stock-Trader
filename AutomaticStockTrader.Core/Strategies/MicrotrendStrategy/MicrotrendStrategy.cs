﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository;

namespace AutomaticStockTrader.Core.Strategies.MicrotrendStrategy
{
    public class MicrotrendStrategy : IStrategy
    {
        public Task<bool?> ShouldBuyStock(IList<StockInput> HistoricalData)
        {
            var last3Values = HistoricalData.OrderByDescending(x => x.Time).Take(3).Select(x => x.ClosingPrice).ToList();

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
