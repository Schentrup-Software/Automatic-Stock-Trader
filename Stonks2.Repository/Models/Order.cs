using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomaticStockTrader.Repository.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Strategy { get; set; }

        public DateTime OrderPlaced { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string StockSymbol { get; set; }

        public decimal? SharesBought { get; set; }

        public decimal? CostPerShare { get; set; }
    }
}
