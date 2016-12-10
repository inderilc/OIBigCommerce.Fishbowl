using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigCommerce4Net;
using BigCommerce4Net.Api;
using BigCommerce.Fishbowl.Configuration;
using BigCommerce4Net.Domain;

namespace BigCommerce.Fishbowl.Controller
{
    public class BController
    {
        private Config cfg;

        public event LogMsg OnLog;
        public delegate void LogMsg(String msg);
        private FishbowlController fb { get; set; }

        public BController(Config cfg)
        {
            this.cfg = cfg;
        }

        public List<Order> GetOrders(string v)
        {
            var Api_Configuration = new BigCommerce4Net.Api.Configuration()
            {
                ServiceURL = cfg.Store.StoreUrl,
                UserName = cfg.Store.UserName,
                UserApiKey = cfg.Store.ApiKey,
                MaxPageLimit = 250,
                AllowDeletions = true // Is false by default, must be true to allow deletions
            };

            var client = new Client(Api_Configuration);
            //var rs = client.OrderStatuses.Get();
            var rso = client.Orders.Get(new FilterOrders() {StatusId=11});

            return rso.Data;
        }

    }
}
