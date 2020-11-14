using Alpaca.Markets;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using Stonks2.Configuration;
using System;
using System.Linq;


//Config

var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json", true, true)
    .AddEnvironmentVariables()
    .Build()
    .Get<AlpacaConfig>();


//Get data from Alpaca api

using var env = config.Run_In_Production 
    ? Environments.Live.GetAlpacaDataClient(new SecretKey(config.Live_Key_Id, config.Secret_Key) )
    : Environments.Paper.GetAlpacaDataClient(new SecretKey(config.Paper_Key_Id, config.Secret_Key));
using var polygonClient = Environments.Live.GetPolygonDataClient(config.Live_Key_Id);

var stockData = await polygonClient.ListAggregatesAsync(
    new AggregatesRequest("GE", new AggregationPeriod(1, AggregationPeriodUnit.Minute))
    .SetInclusiveTimeInterval(DateTime.Now.AddDays(-7), DateTime.Now));

//Transform into diffrences

var rawTrainData = stockData.Items.Where(x => x.TimeUtc < DateTime.UtcNow.AddDays(-1)).ToList();   
var trainData = rawTrainData.Select((x, i) => i == 0 ?  0 : x.Close - rawTrainData[i - 1].Close);

var rawTestData = stockData.Items.Where(x => x.TimeUtc >= DateTime.UtcNow.AddDays(-1)).ToList();
var testData = rawTrainData.Select((x, i) => i == 0 ? 0 : x.Close - rawTestData[i - 1].Close);

//Create model

var mlContext = new MLContext();
var trainDataVeiw = mlContext.Data.LoadFromEnumerable(rawTrainData);
var testDataVeiw = mlContext.Data.LoadFromEnumerable(rawTestData);



