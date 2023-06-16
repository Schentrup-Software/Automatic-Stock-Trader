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

        [TestInitialize]
        public void SetUp()
        {

            _mockAlpacaTradingClient = new Mock<IAlpacaTradingClient>();
            _mockAlpacaTradingStreamingClient = new Mock<IAlpacaStreamingClient>();
            _mockAlpacaDataClient = new Mock<IAlpacaDataClient>();
            _mockAlpacaDataStreamingClient = new Mock<IAlpacaDataStreamingClient>();

            _alpacaClient = new AlpacaClient(
                _mockAlpacaTradingClient.Object,
                _mockAlpacaTradingStreamingClient.Object,
                _mockAlpacaDataClient.Object,
                _mockAlpacaDataStreamingClient.Object
            );
        }

        [TestMethod]
        public async Task PlaceOrder_PositiveShares_Buys()
        {
            var order = new Order
            {
                StockSymbol = "Foo",
                MarketPrice = 12m,
                OrderPlacedTime = DateTime.Now,
                SharesBought = 13
            };

            await _alpacaClient.PlaceOrder(order);

            _mockAlpacaTradingClient.Verify(x => x.PostOrderAsync(It.Is<NewOrderRequest>(y =>
                y.Symbol == order.StockSymbol &&
                y.Quantity.Value == order.SharesBought &&
                y.Side == OrderSide.Buy &&
                y.Type == Alpaca.Markets.OrderType.Market &&
                y.Duration == TimeInForce.Ioc
            ), default), Times.Once);
        }

        [TestMethod]
        public async Task PlaceOrder_NegativeShares_Sells()
        {
            var order = new Order
            {
                StockSymbol = "Foo",
                MarketPrice = 12m,
                OrderPlacedTime = DateTime.Now,
                SharesBought = -13
            };

            await _alpacaClient.PlaceOrder(order);

            _mockAlpacaTradingClient.Verify(x => x.PostOrderAsync(It.Is<NewOrderRequest>(y =>
                y.Symbol == order.StockSymbol &&
                y.Quantity.Value == order.SharesBought * (-1) &&
                y.Side == OrderSide.Sell &&
                y.Type == Alpaca.Markets.OrderType.Market &&
                y.Duration == TimeInForce.Ioc
            ), default), Times.Once);
        }
    }
}
