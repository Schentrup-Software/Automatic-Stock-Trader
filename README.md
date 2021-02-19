# Automatic Stock Trader

A framework for building and testing minute to minute automatic trading using the [Alpaca API](https://alpaca.markets/).

## How to run

### Run using Digital Ocean

Click the button below! You can customize the strategy and stock list using env vars on deployment (examples below). You will need to add your own Alpaca app ids and secret keys to the env vars through the UI.

 [![Deploy to DO](https://mp-assets1.sfo2.digitaloceanspaces.com/deploy-to-do/do-btn-blue.svg)](https://cloud.digitalocean.com/apps/new?repo=https://github.com/Schentrup-Software/Automatic-Stock-Trader/tree/master)
 
### Locally or on another server

This is a command line tool written in .NET 5. After compiling the project using Visual Studio or .NET cli,
you will need to add the follwing enviroment variables:

```
"ALPACA_SECRET_KEY": "Your personal Alpaca secret key",
"ALPACA_APP_ID": "Your personal Alpaca app id",
```

The command app runs on the paper api by default. If you want to run the app using a live account, you need to set another enviorment variable `ALPACA_USE_LIVE_API` to `true`.

To set the strategy to be used and the stocks the strategy should be run on, you will need to do one of two things:

1. Pass in the values as command line arguments. Ex. `stonks2 --stock-symbols GE F AAPL --strategy-names MeanReversionStrategy MicrotrendStrategy --trading-freqencies Minute Day`
2. Pass the values in via a environment variables. Ex. `export STOCK_LIST="GE, F, AAPL" && export TRADING_STRATEGIES="MeanReversionStrategy, MicrotrendStrategy && export TRADING_FREQENCIES="Minute, Day"`

The two examples above are essentially equivalent. They employ the "MeanReversionStrategy" every minute and the "MicrotrendStrategy" every day for the stocks GE, F, and AAPL.

## Available strategies

As the trading frequency is variable, the units are then variable. Thus, "units" as used below are the used trading frequency (Minute, Hour, Day, etc.)

### MeanReversionStrategy

Risk: :dragon_face: :dragon_face:
Possible reward: :moneybag: :moneybag:

Calculates the average price for the past 20 units. If the price is lower than the average, buy the stock. If
the price is above the average, sell the position.

### MirotrendStrategy

Risk: :dragon_face:
Possible reward: :moneybag:

If the price increases for 2 conseutive minutes, buy the stock. If the price has dropped in the past 2 units, sell the
position. If the price remained the same, do not buy or sell.

### MLStategy

Risk: :dragon_face: :dragon_face: :dragon_face:
Possible reward: :moneybag:

Uses ML.Net to create a forecasting model using Singular Spectrum Analysis on the past days price movements. After the 
model is trained, we use to predict the nex minutes price. If the prediction is the price will go up, we buy. If we predict
the price will go down, we sell. This training and prediction happens every trading cycle. 
