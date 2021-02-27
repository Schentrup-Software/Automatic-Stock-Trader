using Alpaca.Markets;
using AutomaticStockTrader.Core.Alpaca;
using AutomaticStockTrader.Core.Configuration;
using AutomaticStockTrader.Domain;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace AutomaticStockTrader.Tests
{
    [TestClass, TestCategory("Small")]
    public class AlpacaClientTests
    {
        private AlpacaClient _alpacaClient;

        private Mock<IAlpacaTradingClient> _mockAlpacaTradingClient;
        private Mock<IAlpacaStreamingClient> _mockAlpacaTradingStreamingClient;
        private Mock<IAlpacaDataClient> _mockAlpacaDataClient;
        private Mock<IAlpacaDataStreamingClient> _mockAlpacaDataStreamingClient;
        private AlpacaConfig _testConfig;

        [TestInitialize]
        public void SetUp()
        {

            _mockAlpacaTradingClient = new Mock<IAlpacaTradingClient>();
            _mockAlpacaTradingStreamingClient = new Mock<IAlpacaStreamingClient>();
            _mockAlpacaDataClient = new Mock<IAlpacaDataClient>();
            _mockAlpacaDataStreamingClient = new Mock<IAlpacaDataStreamingClient>();

            _testConfig = new AlpacaConfig
            {
                Alpaca_App_Id = "p243ifj23-9fjipwfn4pjinf24e",
                Alpaca_Secret_Key = "02489gh230gh-93fqenpi",
                Alpaca_Use_Live_Api = false,
            };

            _alpacaClient = new AlpacaClient(
                Options.Create(_testConfig),
                _mockAlpacaTradingClient.Object,
                _mockAlpacaTradingStreamingClient.Object,
                _mockAlpacaDataClient.Object,
                _mockAlpacaDataStreamingClient.Object
            );
        }

        [TestMethod]
        public async Task PlaceOrder_PositiveShares_Buys()
        {
            var strategyStock = new StrategysStock
            {
                StockSymbol = "Foo",
                Strategy = "Bar",
                TradingFrequency = TradingFrequency.Year
            };
            var order = new Order
            {
                MarketPrice = 12m,
                OrderPlacedTime = DateTime.Now,
                SharesBought = 13
            };

            await _alpacaClient.PlaceOrder(strategyStock, order);

            _mockAlpacaTradingClient.Verify(x => x.PostOrderAsync(It.Is<NewOrderRequest>(y =>
                y.Symbol == strategyStock.StockSymbol &&
                y.Quantity == order.SharesBought &&
                y.Side == OrderSide.Buy &&
                y.Type == OrderType.Market &&
                y.Duration == TimeInForce.Ioc
            ), default), Times.Once);
        }

        [TestMethod]
        public async Task PlaceOrder_NegativeShares_Sells()
        {
            var strategyStock = new StrategysStock
            {
                StockSymbol = "Foo",
                Strategy = "Bar",
                TradingFrequency = TradingFrequency.Year
            };
            var order = new Order
            {
                MarketPrice = 12m,
                OrderPlacedTime = DateTime.Now,
                SharesBought = -13
            };

            await _alpacaClient.PlaceOrder(strategyStock, order);

            _mockAlpacaTradingClient.Verify(x => x.PostOrderAsync(It.Is<NewOrderRequest>(y =>
                y.Symbol == strategyStock.StockSymbol &&
                y.Quantity == order.SharesBought * (-1) &&
                y.Side == OrderSide.Sell &&
                y.Type == OrderType.Market &&
                y.Duration == TimeInForce.Ioc
            ), default), Times.Once);
        }
    }
}
