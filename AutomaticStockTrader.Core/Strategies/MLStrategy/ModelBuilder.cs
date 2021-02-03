using System;
using System.Collections.Generic;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using AutomaticStockTrader.Core.Configuration;

namespace AutomaticStockTrader.Core
{
    public class ModelBuilder
    {
        private MLConfig _config;

        public ModelBuilder(MLConfig config)
        {
            _config = config;
        }

        public TimeSeriesPredictionEngine<ModelInput, ModelOutput> BuildModel(IList<ModelInput> inputs)
        {
            //Create model
            var mlContext = new MLContext();
            var trainDataVeiw = mlContext.Data.LoadFromEnumerable(inputs);

            var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "ForecastedPriceDiffrence",
                inputColumnName: "PriceDiffrence",
                windowSize: _config.Window_Size,
                seriesLength: _config.Series_Length,
                trainSize: inputs.Count,
                horizon: _config.Horizon,
                confidenceLevel: _config.Confidence_Level,
                confidenceLowerBoundColumn: "LowerBoundPriceDiffrence",
                confidenceUpperBoundColumn: "UpperBoundPriceDiffrence");

            var forecaster = forecastingPipeline.Fit(trainDataVeiw);
            return forecaster.CreateTimeSeriesEngine<ModelInput, ModelOutput>(mlContext);
        }
    }

    public record ModelInput
    {
        public float PriceDiffrence { get; init; }
        public DateTime Time { get; init; }
    }

    public record ModelOutput
    {
        public float[] ForecastedPriceDiffrence { get; init; }
        public float[] LowerBoundPriceDiffrence { get; init; }
        public float[] UpperBoundPriceDiffrence { get; init; }
    }
}
