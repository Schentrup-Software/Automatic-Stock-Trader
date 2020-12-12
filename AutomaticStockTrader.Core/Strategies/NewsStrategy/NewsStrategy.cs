using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Domain;
using AutomaticStockTrader.Repository;

namespace AutomaticStockTrader.Core.Strategies.NewsStrategy
{
    public class NewsStrategy : Strategy
    {
        private readonly NewsSearchConfig _config;

        public NewsStrategy(IAlpacaClient client, ITrackingRepository trackingRepository, decimal percentageOfEquityToAllocate, NewsSearchConfig config) 
            : base(client, trackingRepository, TradingFrequency.Day, percentageOfEquityToAllocate)
        {
            _config = config;
        }

        public async Task DoStuff()
        {
            var client = new NewsSearchClient(new ApiKeyServiceClientCredentials(_config.News_Search_Api_Key))
            {
                Endpoint = _config.News_Search_Endpoint
            };

            using var tw = new StreamWriter("SavedList.txt");

            for (int i = 0; i < 10; i++)
            {
                var result = await client.News.CategoryAsync(market: "en-US", count: 100, category: "Business", offset: 100 * i);
                foreach (string s in result.Value.Select(x => x.Name))
                    tw.WriteLine(s);
            }
        }

        public override async Task<bool?> ShouldBuyStock(StockInput newData)
        {
            var client = new NewsSearchClient(new ApiKeyServiceClientCredentials(_config.News_Search_Api_Key))
            {
                Endpoint = _config.News_Search_Endpoint
            };

            var result = await client.News.SearchAsync(query: newData.StockSymbol, market: "en-US", freshness: "Day", count: 100);
            return false;
        }
    }
}
