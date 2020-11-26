using AutomaticStockTrader.Repository.Configuration;
using Microsoft.EntityFrameworkCore;
using System;

namespace AutomaticStockTrader.Repository.Models
{
    public class DatabaseContext : DbContext
    {
        private readonly DatabaseConfig _config;

        public DbSet<Order> Orders { get; set; }

        public DatabaseContext(DatabaseConfig config)
        {
            _config = config;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (string.IsNullOrWhiteSpace(_config.Db_Connection_String))
            {
                optionsBuilder.UseSqlite("Data Source=:memory:");
            }
            else 
            {
                optionsBuilder.UseSqlServer(_config.Db_Connection_String);
            }
        }
    }
}
