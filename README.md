# Automatic Stock Trader

A framework for building and testing minute to minute automatic trading using the [Alpaca API](https://alpaca.markets/).

## How to run

This is a command line tool written in .NET 5. After compiling the project using Visual Studio or .NET cli,
you will need to add the follwing enviroment variables:

```
"SECRET_KEY": "Alpaca secret key",
"PAPER_KEY_ID": "Alpaca paper app id",
"LIVE_KEY_ID": "Alpaca live app id",
```

The command app runs on the paper api by default. A live app id is still required if using the paper api. If you 
want to run the app using a live account, you need to set the `SECRET_KEY` to your live app secret key, another 
enviorment variable `Run_In_Production` to `true`, and there is no need to set the `PAPER_KEY_ID`.

## Available strategies

### Mean Reversion Strategy

Calculates the average price for the past 20 min. If the price is lower than the average, buy the stock. If
the price is above the average, sell the position.

### Mirotrend Strategy

If the price increases for 2 conseutive minutes, buy the stock. If the price has dropped in the past 2 min, sell the
position. If the price remained the same, do not buy or sell.

### Machine Learning Stategy

Uses ML.Net to create a forecasting model using Singular Spectrum Analysis on the past days price movements. After the 
model is trained, we use to predict the nex minutes price. If the prediction is the price will go up, we buy. If we predict
the price will go down, we sell. This training and prediction happens every minute. 

### News Strategy (in developement)

Uses the Bing news search api to search that days news for headlines containing the stock symbol. Those headlines are
put through a ML model to detect how positive they are. If the news is positive, we buy the stock for the day. If the
news is negative overall, sell the stock.
