using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AutomaticStockTrader.Tests.Tracking
{
    public static class TrackingRecordWriter
    {
        private const string FILE_PATH = "../../../Tracking/tracking.csv";

        public static void WriteData(TrackingRecord record)
        {
            if (File.Exists(FILE_PATH))
            {
                using var stream = File.Open(FILE_PATH, FileMode.Append);
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false
                });

                csv.WriteRecords(new List<TrackingRecord> { record });
                writer.Flush();
            }
            else
            {
                using var writer = new StreamWriter(FILE_PATH);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                csv.WriteRecords(new List<TrackingRecord> { record });
                writer.Flush();
            }
        }
    }
}
