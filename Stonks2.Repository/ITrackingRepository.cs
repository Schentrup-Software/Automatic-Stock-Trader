using System;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Repository
{
    public interface ITrackingRepository : IDisposable
    {
        public Task AddPendingOrder(Domain.StrategysStock postion, Domain.Order order);
        public Task CompleteOrder(string stockSymbol, decimal price, long sharesBought);
        public Task<Domain.Position> GetOrCreateEmptyPosition(Domain.StrategysStock strategysStock);
    }
}
