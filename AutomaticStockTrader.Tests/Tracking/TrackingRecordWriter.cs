using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace AutomaticStockTrader.Tests.Tracking
{
    public static class TrackingRecordWriter
    {
        private const string COLLECTION = "tracking";

        public static async Task WriteData(TrackingConfig config, double percentageMade, string strategyName)
        {
            Debug.Write($"Google app creds: {config.Google_Application_Credentials}");
            if (!string.IsNullOrWhiteSpace(config.Google_Application_Credentials))
            {
                await (await FirestoreDb.CreateAsync("automatic-stock-trader-tracker")).Collection(COLLECTION).AddAsync(new { DateTime.UtcNow.Date, percentageMade, strategyName });
            }
        }
    }
}
