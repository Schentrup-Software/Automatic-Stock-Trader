namespace AutomaticStockTrader.Repository.Configuration
{
    public class DatabaseConfig
    {
        public string Db_Connection_String { get; set; } = "Server=localhost\\SQLEXPRESS;Database=AutomaticStockTrader_Stocks;Trusted_Connection=True;";
    }
}
