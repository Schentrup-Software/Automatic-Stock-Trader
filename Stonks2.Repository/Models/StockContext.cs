using AutomaticStockTrader.Repository.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AutomaticStockTrader.Repository.Models
{
    public class StockContext : DbContext
    {
        private readonly DatabaseConfig _config;

        public DbSet<Order> Orders { get; set; }
        public DbSet<StratagysStock> StratagysStocks { get; set; }

        public StockContext(DatabaseConfig config = null)
        {
            _config = config ?? new DatabaseConfig();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_config.Db_Connection_String);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<StratagysStock>()
                .HasIndex(p => new { p.StockSymbol, p.Strategy, p.TradingFrequency })
                .IsUnique();
        }
    }
}
