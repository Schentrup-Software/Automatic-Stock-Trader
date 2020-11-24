using System.Collections.Generic;
using System.Linq;

namespace Stonks2.Configuration
{
    public class StockConfig
    {
        public string Stock_Strategy { get; set; }
        public string Stock_List_Raw { get; set; }
        public IList<string> Stock_List => Stock_List_Raw.Split(',').Select(x => x.Trim()).ToList();
    }
}
