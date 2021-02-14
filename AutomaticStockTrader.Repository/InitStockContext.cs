using AutomaticStockTrader.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Repository
{
    public class InitStockContext : IHostedService
    {
        private readonly StockContext _context;

        public InitStockContext(StockContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_context.IsUsingDefault)
            {
                await _context.Database.EnsureDeletedAsync(cancellationToken);
                await _context.Database.EnsureCreatedAsync(cancellationToken);
            }
            else
            {
                await _context.Database.MigrateAsync(cancellationToken: cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
