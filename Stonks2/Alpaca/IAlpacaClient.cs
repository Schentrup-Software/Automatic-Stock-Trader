using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Alpaca
{
    public interface IAlpacaClient : IDisposable
    {
        public Task<bool> ConnectStreamApi();
        public void SubscribeMinuteAgg(string stockSymbol, Action<StockInput> action);
        public Task<decimal> EnsurePositionExists(string stockSymbol, decimal marketValue);
        public Task<decimal> EnsurePostionCleared(string stockSymbol);
        public Task<IList<StockInput>> GetStockData(string stockSymbol);
    }
}
