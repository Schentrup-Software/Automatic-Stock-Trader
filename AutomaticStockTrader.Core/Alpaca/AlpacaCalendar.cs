using Quartz.Impl.Calendar;
using System;

namespace AutomaticStockTrader.Core.Alpaca
{
    public class AlpacaCalendar : BaseCalendar
    {
        private readonly IAlpacaClient _alpacaClient;

        public AlpacaCalendar(IAlpacaClient alpacaClient)
        {
            _alpacaClient = alpacaClient ?? throw new ArgumentNullException(nameof(alpacaClient));
        }

        public override DateTimeOffset GetNextIncludedTimeUtc(DateTimeOffset timeUtc)
            => _alpacaClient.GetNextIncludedTimeUtc(timeUtc).Result;


        public override bool IsTimeIncluded(DateTimeOffset timeUtc)
            => _alpacaClient.IsTimeIncluded(timeUtc).Result;
    }
}
