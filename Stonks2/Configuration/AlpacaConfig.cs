using System.Collections.Generic;
using System.Linq;

namespace Stonks2.Configuration
{
    public class AlpacaConfig
    {
        public bool Run_In_Production { get; set; }
        public string Secret_Key { get; set; }
        public string Paper_Key_Id { get; set; }
        public string Live_Key_Id { get; set; }
        //Minute = 0, Hour = 1, Day = 2, Week = 3, Month = 4, Quarter = 5, Year = 6
        public int Aggregation_Period_Unit { get; set; }
        public double Days_To_Look_Back { get; set; }
        public string Stock_List_Raw { get; set; }
        public IEnumerable<string> Stock_List => Stock_List_Raw.Split(',').Select(x => x.Trim());
        public decimal Percentage_Of_Equity_Per_Position { get; set; }
    }
}
