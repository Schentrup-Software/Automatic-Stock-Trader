using AutomaticStockTrader.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomaticStockTrader.Core.Configuration
{
    public class StrategyConfig
    {
        public string Trading_Strategies { get; set; }
        public IList<string> Trading_Strategies_Parsed => Trading_Strategies?.Split(',')?.Select(x => x.Trim())?.ToList();
        public string Trading_Freqencies { get; set; }
        public IList<TradingFrequency> Trading_Freqencies_Parsed => Trading_Freqencies?.Split(',')?.Select(x => Enum.Parse<TradingFrequency>(x.Trim()))?.ToList();
        public decimal Percentage_Of_Equity_Per_Position { get; set; }
        public string Stock_List { get; set; }
        public IList<string> Stock_List_Parsed => Stock_List?.Split(',')?.Select(x => x.Trim())?.ToList();
    }
}
