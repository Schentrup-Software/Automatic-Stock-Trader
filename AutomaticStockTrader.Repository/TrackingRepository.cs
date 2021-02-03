using AutomaticStockTrader.Repository.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Repository
{
    public class TrackingRepository : ITrackingRepository
    {
        private readonly StockContext _context;

        public TrackingRepository(StockContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddPendingOrder(Domain.StrategysStock strategysStock, Domain.Order order)
        {
            var stratagysStock = GetStatagysStockFromDb(strategysStock);

            _context.Add(new Order
            {
                OrderPlaced = order.OrderPlacedTime,
                AttemptedCostPerShare = order.MarketPrice,
                AttemptedSharesBought = order.SharesBought,
                PositionId = stratagysStock.Id
            });

            await _context.SaveChangesAsync();
        }

        public async Task CompleteOrder(string stockSymbol, decimal price, long sharesBought)
        {
            var potentialOrders = _context.Orders
                .Where(x =>
                    !x.ActualCostPerShare.HasValue &&
                    !x.ActualSharesBought.HasValue &&
                    x.Position.StockSymbol == stockSymbol)
                .Select(x => new { ShareDiff = Math.Abs(x.AttemptedSharesBought - sharesBought), Value = x })
                .OrderBy(x => x.ShareDiff)
                .ToList();

            if (potentialOrders.Any())
            {
                // If there are more than one portential orders, we just want to pick one closet to the attempted shares bought and assign it.
                var order = potentialOrders.Select(x => x.Value).First(); 

                order.ActualCostPerShare = price;
                order.ActualSharesBought = sharesBought;

                await _context.SaveChangesAsync();
            } 
            else if (potentialOrders.Count > 1)
            {
                Console.WriteLine("Failed to find potential order that matched. Ignoring for now");
            }
        }

        public async Task<Domain.Position> GetOrCreateEmptyPosition(Domain.StrategysStock strategysStock)
        {
            var strategysStockDb = GetStatagysStockFromDb(strategysStock);

            if (strategysStockDb == null)
            {
                _context.StratagysStocks.Add(new StratagysStock
                {
                    StockSymbol = strategysStock.StockSymbol,
                    Strategy = strategysStock.Strategy,
                    TradingFrequency = strategysStock.TradingFrequency
                });

                await _context.SaveChangesAsync();
                strategysStockDb = GetStatagysStockFromDb(strategysStock);
            }

            return new Domain.Position
            {
                StockSymbol = strategysStockDb.StockSymbol,
                NumberOfShares = strategysStockDb?.Orders
                        ?.Select(x => x.ActualSharesBought ?? 0)
                        ?.Aggregate((x, y) => x + y) ?? 0
            };
        }

        private StratagysStock GetStatagysStockFromDb(Domain.StrategysStock strategysStock)
            => _context.StratagysStocks.SingleOrDefault(x =>
                x.StockSymbol == strategysStock.StockSymbol &&
                x.Strategy == strategysStock.Strategy &&
                x.TradingFrequency == strategysStock.TradingFrequency);

        public void Dispose()
            => _context?.Dispose();
    }
}
