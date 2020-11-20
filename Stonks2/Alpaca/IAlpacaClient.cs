using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stonks2.Alpaca
{
    public interface IAlpacaClient : IDisposable
    {
        public Task<bool> ConnectStreamApi(string stockSymbol);
        public void SubscribeMinuteAgg(string stockSymbol, Action<StockInput> action);
        public Task<decimal> EnsurePositionExists(string stockSymbol, decimal marketValue);
        public Task<decimal> EnsurePostionCleared(string stockSymbol);
        public Task<IList<StockInput>> GetStockData(string stockSymbol);
    }
}
