using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigCommerce4Net;
using BigCommerce4Net.Api;

namespace TestBC
{
    class Program
    {
        static void Main(string[] args)
        {
            var Api_Configuration = new BigCommerce4Net.Api.Configuration()
            {
                ServiceURL = "https://store-o6fevuv0k8.mybigcommerce.com/api/v2/",
                UserName = "fishbowl",
                UserApiKey = "ccdce80a820410b96d948b0b3be7f95fa419be75",
                MaxPageLimit = 250,
                AllowDeletions = true // Is false by default, must be true to allow deletions
            };

            var c = new Client(Api_Configuration);

            var rs = c.OrderStatuses.Get();
            var rso = c.Orders.Get(new FilterOrders() { });

            int i = 1 + 1;
        }
    }
}
