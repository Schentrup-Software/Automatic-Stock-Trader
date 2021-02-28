using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Repository
{
    public interface ITrackingRepository : IDisposable
    {
        public Task AddPendingOrder(Domain.StrategysStock postion, Domain.Order order);
        public Task CompleteOrder(Domain.Order completedOrder);
        public Task AddOrder(Domain.StrategysStock strategysStock, Domain.Order order);
        public Task<Domain.Position> GetOrCreateEmptyPosition(Domain.StrategysStock strategysStock);
        public IEnumerable<Domain.Order> GetCompletedOrders(Domain.StrategysStock strategysStock);
    }
}
