using System;
using System.ComponentModel.DataAnnotations;

namespace AutomaticStockTrader.Repository.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; }

        public Guid PositionId { get; set; }

        public DateTimeOffset OrderPlaced { get; set; }

        public long AttemptedSharesBought { get; set; }

        public decimal AttemptedCostPerShare { get; set; }

        public long? ActualSharesBought { get; set; }

        public decimal? ActualCostPerShare { get; set; }

        public virtual StratagysStock Position { get; set; }
    }
}
