using System.Collections.Generic;
using System.Linq;

namespace AutomaticStockTrader.Configuration
{
    public class AlpacaConfig
    {
        public bool Alpaca_Use_Live_Api { get; set; }
        public string Alpaca_Paper_Secret_Key { get; set; }
        public string Alpaca_Paper_App_Id { get; set; }
        public string Alpaca_Live_App_Id { get; set; }
        public string Alpaca_Live_Secret_Key { get; set; }
        //Minute = 0, Hour = 1, Day = 2, Week = 3, Month = 4, Quarter = 5, Year = 6
        public int Aggregation_Period_Unit { get; set; }
        public int Number_Of_Minutes_To_Look_Back { get; set; }
        public decimal Percentage_Of_Equity_Per_Position { get; set; }
    }
}
