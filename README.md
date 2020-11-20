# AutomaticStockTrader

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
enviorment variable `Run_In_Production` to `true, and there is no need to set the `PAPER_KEY_ID`.
