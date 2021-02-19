using AutomaticStockTrader.Domain;
using System.Collections.Generic;
using System.Linq;

namespace AutomaticStockTrader.Core.Configuration
{
    public class AlpacaConfig
    {
        public bool Alpaca_Use_Live_Api { get; set; }
        public string Alpaca_Secret_Key { get; set; }
        public string Alpaca_App_Id { get; set; }
        public TradingFrequency Aggregation_Period_Unit { get; set; }
        public int Number_Of_Units_To_Look_Back { get; set; }

        public string Trading_Start_Time { get; set; }
        public IReadOnlyList<int> Trading_Start_Time_Parsed => Trading_Start_Time.Split(':').Select(x => int.Parse(x)).ToList();
        public string Trading_Stop_Time { get; set; }
        public IReadOnlyList<int> Trading_Stop_Time_Parsed => Trading_Start_Time.Split(':').Select(x => int.Parse(x)).ToList();
    }
}
