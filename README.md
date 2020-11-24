# Automatic Stock Trader

A framework for building and testing minute to minute automatic trading using the [Alpaca API](https://alpaca.markets/).

## How to run

This is a command line tool written in .NET 5. After compiling the project using Visual Studio or .NET cli,
you will need to add the follwing enviroment variables:

```
"ALPACA_SECRET_KEY": "Alpaca secret key",
"ALPACA_KEY_ID": "Alpaca app id",
```

The command app runs on the paper api by default. If you want to run the app using a live account, you need to set another enviorment variable `Alpaca_Use_Live_Api` to `true`.

To set the strategy to be used and the stocks the strategy should be run on, you will need to do one of two things:

1. Pass in the values as command line arguments. Ex. `stonks2 --stock-symbols-args GE F AAPL --strategy-name-args MeanReversionStrategy`
2. Pass the values in via a environment variables. Ex. `export Stock_List_Raw="GE, F, AAPL" && export Stock_Strategy="MeanReversionStrategy"`

## Available strategies

### MeanReversionStrategy

Calculates the average price for the past 20 min. If the price is lower than the average, buy the stock. If
the price is above the average, sell the position.

### MirotrendStrategy

If the price increases for 2 conseutive minutes, buy the stock. If the price has dropped in the past 2 min, sell the
position. If the price remained the same, do not buy or sell.

### MLStategy

Uses ML.Net to create a forecasting model using Singular Spectrum Analysis on the past days price movements. After the 
model is trained, we use to predict the nex minutes price. If the prediction is the price will go up, we buy. If we predict
the price will go down, we sell. This training and prediction happens every minute. 

### News Strategy (in developement)

Uses the Bing news search api to search that days news for headlines containing the stock symbol. Those headlines are
put through a ML model to detect how positive they are. If the news is positive, we buy the stock for the day. If the
news is negative overall, sell the stock.
