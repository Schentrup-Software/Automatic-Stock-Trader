using Alpaca.Markets;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using Stonks2;
using Stonks2.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

var _quitEvent = new ManualResetEvent(false);

Console.CancelKeyPress += (sender, eArgs) => {
    _quitEvent.Set();
    eArgs.Cancel = true;
};

//Config
var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json", true, true)
    .AddEnvironmentVariables()
    .Build();

var allStocksHistoricalData = new Dictionary<string, IList<ModelInput>>();
var alpacaConfig = config.Get<AlpacaConfig>();
using var alpacaClient = new AlpacaClient(alpacaConfig);

foreach (var stockSymbol in alpacaConfig.Stock_List)
{
    var trainData = await alpacaClient.GetStockData(stockSymbol);
    allStocksHistoricalData.Add(stockSymbol, trainData);   
}

if(!await alpacaClient.ConnectStreamApi())
{
    throw new UnauthorizedAccessException("Failed to connect to streaming API. Authorization failed.");
}

foreach (var stockData in allStocksHistoricalData)
{
    await alpacaClient.SubscribeToQuoteChange(stockData.Key, x =>
    {
        if (allStocksHistoricalData.TryGetValue(stockData.Key, out var inputs))
        {
            inputs.Add(new ModelInput
            {
                PriceDiffrence = (float)((x.Close - inputs.Last().ClosingPrice) / inputs.Last().ClosingPrice),
                ClosingPrice = x.Close,
                Time = x.EndTimeUtc
            });
            var modelBuilder = new ModelBuilder(config.Get<MLConfig>());
            var model = modelBuilder.BuildModel(inputs);
            var result = model.Predict();
            result.ForecastedPriceDiffrence.ToList().ForEach(x => Console.WriteLine(x));

            if(result.ForecastedPriceDiffrence[0] > 0)
            {
                alpacaClient.TakePosition(stockData.Key).Wait();
            }
            else
            {
                alpacaClient.ClearPostion(stockData.Key).Wait();
            }
        }        
    });
}

_quitEvent.WaitOne();
