using AutomaticStockTrader.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Repository
{
    public class TrackingRepository : ITrackingRepository
    {
        private readonly DatabaseContext _context;

        public TrackingRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task AddOrder(string strategy, string stockSymbol, DateTime orderPlacedTime)
        {
            _context.Add(new Order
            {
                OrderPlaced = orderPlacedTime,
                StockSymbol = stockSymbol,
                Strategy = strategy,
            });

            await _context.SaveChangesAsync();
        }
        public IEnumerable<Order> GetOrders()
        {
            return _context.Orders;
        }

        public void Dispose()
            => _context?.Dispose();
    }
}
