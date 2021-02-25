using AutomaticStockTrader.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Repository
{
    public class InitStockContext : IHostedService
    {
        private readonly ILogger<InitStockContext> _logger;
        private readonly StockContext _context;

        public InitStockContext(ILogger<InitStockContext> logger, StockContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {GetType().Name} job");

            if (_context.IsUsingDefault)
            {
                _logger.LogInformation("Using default SQLite DB context");
                await _context.Database.OpenConnectionAsync();
                await _context.Database.EnsureDeletedAsync(cancellationToken);
                await _context.Database.EnsureCreatedAsync(cancellationToken);
            }
            else
            {
                _logger.LogInformation("Using provided SQL Server DB context");
                await _context.Database.MigrateAsync(cancellationToken: cancellationToken);
            }

            _logger.LogInformation($"Finished {GetType().Name} job");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_context.IsUsingDefault)
            {
                _logger.LogInformation("Deleting default SQLite DB context");
                await _context.Database.EnsureDeletedAsync(cancellationToken);
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}
