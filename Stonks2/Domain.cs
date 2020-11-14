using System;
using Microsoft.ML.Data;

namespace Stonks2
{
    public record ModelInput
    {
        public float PriceDiffrence { get; init; }
        public DateTime Time { get; init; }

        [NoColumn]
        public decimal ClosingPrice { get; init; }
    }

    public record ModelOutput 
    {
        public float[] ForecastedPriceDiffrence { get; init; }
        public float[] LowerBoundPriceDiffrence { get; init; }
        public float[] UpperBoundPriceDiffrence { get; init; }
    }
}
