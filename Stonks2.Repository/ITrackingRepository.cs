using AutomaticStockTrader.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Repository
{
    public interface ITrackingRepository : IDisposable
    {
        public Task AddOrder(string strategy, string stockSymbol, DateTime orderPlacedTime);
        public IEnumerable<Order> GetOrders();
    }
}
