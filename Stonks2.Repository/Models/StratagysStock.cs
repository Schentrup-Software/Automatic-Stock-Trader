using AutomaticStockTrader.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomaticStockTrader.Repository.Models
{
    public class StratagysStock
    {
        [Key]
        public Guid Id { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Strategy { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string StockSymbol { get; set; }

        public TradingFrequency TradingFrequency { get; set; }

        public virtual IEnumerable<Order> Orders { get; set; }
    }
}
