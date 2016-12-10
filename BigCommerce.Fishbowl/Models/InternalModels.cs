using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FishbowlSDK;
using BigCommerce4Net.Domain;

namespace BigCommerce.Fishbowl.Models
{
    public class BCFBOrder
    {
        public String CustomerName { get; set; }
        public Order BCOrder { get; set; }
        public FishbowlSDK.SalesOrder FbOrder { get; set; }
    }
    
    public class SimpleList
    {
        public String Name { get; set; }
        public Boolean InEB { get; set; }
        public Boolean InFB { get; set; }
    }

}
