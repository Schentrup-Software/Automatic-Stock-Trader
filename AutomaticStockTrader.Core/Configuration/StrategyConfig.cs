using AutomaticStockTrader.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomaticStockTrader.Core.Configuration
{
    public class StrategyConfig
    {
        public string Trading_Strategies_Raw { get; set; }
        public IList<string> Trading_Strategies => Trading_Strategies_Raw?.Split(',')?.Select(x => x.Trim())?.ToList();
        public string Trading_Freqencies_Raw { get; set; }
        public IList<TradingFrequency> Trading_Freqencies => Trading_Freqencies_Raw?.Split(',')?.Select(x => Enum.Parse<TradingFrequency>(x.Trim()))?.ToList();
        public decimal Percentage_Of_Equity_Per_Position { get; set; }
    }
}
