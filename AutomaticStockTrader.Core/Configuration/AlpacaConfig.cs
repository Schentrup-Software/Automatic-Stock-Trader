using AutomaticStockTrader.Domain;

namespace AutomaticStockTrader.Core.Configuration
{
    public class AlpacaConfig
    {
        public bool Alpaca_Use_Live_Api { get; set; }
        public string Alpaca_Paper_Secret_Key { get; set; }
        public string Alpaca_Paper_App_Id { get; set; }
        public string Alpaca_Live_App_Id { get; set; }
        public string Alpaca_Live_Secret_Key { get; set; }
        public TradingFrequency Aggregation_Period_Unit { get; set; }
        public int Number_Of_Minutes_To_Look_Back { get; set; }
    }
}
