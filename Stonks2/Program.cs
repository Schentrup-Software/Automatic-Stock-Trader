using Alpaca.Markets;
using Microsoft.Extensions.Configuration;
using Stonks2;
using Stonks2.Alpaca;
using Stonks2.Configuration;
using Stonks2.Stratagies;
using Stonks2.Stratagies.MeanReversionStrategy;
using Stonks2.Stratagies.MicrotrendStrategy;
using Stonks2.Stratagies.MLStrategy;
using Stonks2.Stratagies.NewsStrategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//Config
var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json", true, true)
    .AddEnvironmentVariables()
    .Build();

var stockSymbol = config.Get<StockConfig>().Stock_List.ToList();
var alpacaConfig = config.Get<AlpacaConfig>();
var alpacaClient = new AlpacaClient(alpacaConfig);

var stratagies = new List<Strategy>()
{
    new MLStrategy(alpacaClient, config.Get<MLConfig>()),
    new NewsStrategy(alpacaClient, config.Get<NewsSearchConfig>()),
    new MicrotrendStrategy(alpacaClient),
    new MeanReversionStrategy(alpacaClient),
};

//await new NewsStrategy(config.Get<NewsSearchConfig>()).DoStuff();

await AddStrategy(stockSymbol[0], stratagies[0], alpacaClient);
await AddStrategy(stockSymbol[2], stratagies[2], alpacaClient);
await AddStrategy(stockSymbol[3], stratagies[3], alpacaClient);

while (true)
{
    await Task.Delay(600000);
}

async static Task AddStrategy(string stockSymbol, Strategy strategy, IAlpacaClient alpacaClient)
{
    strategy.HistoricalData = await alpacaClient.GetStockData(stockSymbol);

    if (!await alpacaClient.ConnectStreamApi(stockSymbol))
    {
        throw new UnauthorizedAccessException("Failed to connect to streaming API. Authorization failed.");
    }

    alpacaClient.SubscribeMinuteAgg(stockSymbol, async x => await strategy.HandleMinuteAgg(x));
}
