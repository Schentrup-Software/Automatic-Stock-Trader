using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch;
using Stonks2.Alpaca;
using Stonks2.Configuration;

namespace Stonks2.Stratagies.NewsStrategy
{
    public class NewsStrategy : Strategy
    {
        private readonly NewsSearchConfig _config;
        private DateTime? _lastCallTime;
        private bool _lastReturn;

        public NewsStrategy(IAlpacaClient client, NewsSearchConfig config) : base(client)
        {
            _config = config;
            _lastReturn = false;
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
            if (!_lastCallTime.HasValue || _lastCallTime < DateTime.Now.AddDays(-1))
            {
                var client = new NewsSearchClient(new ApiKeyServiceClientCredentials(_config.News_Search_Api_Key))
                {
                    Endpoint = _config.News_Search_Endpoint
                };

                var result = await client.News.SearchAsync(query: newData.StockSymbol, market: "en-US", freshness: "Day", count: 100);
                return false;
            }
            else
            {
                return _lastReturn;
            }
        }
    }
}
