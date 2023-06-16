using AutomaticStockTrader.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Core.Alpaca
{
    public interface IAlpacaClient : IDisposable
    {
        public Task<bool> ConnectStreamApis();
        public Task DisconnectStreamApis();
        public void AddPendingMinuteAggSubscription(string stockSymbol, Action<StockInput> action);
        public Task SubscribeToMinuteAgg();
        public void SubscribeToTradeUpdates(Action<Order> action);
        public Task PlaceOrder(Order order, OrderTiming? orderTiming = null);
        public Task<decimal?> GetTotalEquity();
        public Task<IEnumerable<Position>> GetPositions();
        public Task<IList<StockInput>> GetStockData(string stockSymbol, TradingFrequency aggUnits, int lookBack = 1000);
        public Task<IEnumerable<DateTime>> GetAllTradingHolidays(DateTime? start = null, DateTime? end = null);
    }
}
