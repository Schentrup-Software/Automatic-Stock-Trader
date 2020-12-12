namespace AutomaticStockTrader.Core.Configuration
{
    public class MLConfig
    {
        public int Window_Size { get; set; }
        public int Series_Length { get; set; }
        public int Horizon { get; set; }
        public float Confidence_Level { get; set; }
    }
}
