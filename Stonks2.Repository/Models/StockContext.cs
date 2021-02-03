using AutomaticStockTrader.Repository.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AutomaticStockTrader.Repository.Models
{
    public class StockContext : DbContext
    {
        private readonly DatabaseConfig _config;

        public DbSet<Order> Orders { get; set; }
        public DbSet<StratagysStock> StratagysStocks { get; set; }

        public bool IsUsingDefault => string.IsNullOrWhiteSpace(_config.Db_Connection_String);

        public StockContext(DatabaseConfig config = null)
        {
            _config = config ?? new DatabaseConfig();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (IsUsingDefault)
            {
                optionsBuilder.UseSqlite(DatabaseConfig.DEFAULT_CONNECTION_STRING);
            }
            else
            {
                optionsBuilder.UseSqlServer(_config.Db_Connection_String);
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<StratagysStock>()
                .HasIndex(p => new { p.StockSymbol, p.Strategy, p.TradingFrequency })
                .IsUnique();
        }
    }
}
