using Alpaca.Markets;

namespace Stonks2
{
    public record ModelInput(decimal PriceDiffrence);

    public record ModelOutput(float[] ForecastedPriceDiffrence, float[] LowerBoundPriceDiffrence, float[] UpperBoundPriceDiffrence);
}
