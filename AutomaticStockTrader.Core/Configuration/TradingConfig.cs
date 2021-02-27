using System.Collections.Generic;
using System.Linq;

namespace AutomaticStockTrader.Core.Configuration
{
    public class TradingConfig
    {
        public bool Is_Pattern_Day_Trader { get; set; }
        public string Trading_Start_Time { get; set; }
        public IReadOnlyList<int> Trading_Start_Time_Parsed => Trading_Start_Time.Split(':').Select(x => int.Parse(x)).ToList();
        public string Trading_Stop_Time { get; set; }
        public IReadOnlyList<int> Trading_Stop_Time_Parsed => Trading_Stop_Time.Split(':').Select(x => int.Parse(x)).ToList();
    }
}
