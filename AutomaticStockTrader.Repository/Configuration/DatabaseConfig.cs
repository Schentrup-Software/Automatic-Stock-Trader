namespace AutomaticStockTrader.Repository.Configuration
{
    public class DatabaseConfig
    {
        public string Db_Connection_String { get; set; }

        public const string DEFAULT_CONNECTION_STRING = @"Data Source=Application;Mode=Memory;Cache=Shared";
    }
}
