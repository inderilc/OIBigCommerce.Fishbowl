﻿using System;
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
        private Client client {get;set;}
        public BController(Config cfg)
        {
            this.cfg = cfg;
            var Api_Configuration = new BigCommerce4Net.Api.Configuration()
            {
                ServiceURL = cfg.Store.StoreUrl,
                UserName = cfg.Store.UserName,
                UserApiKey = cfg.Store.ApiKey,
                MaxPageLimit = 250,
                AllowDeletions = true // Is false by default, must be true to allow deletions
            };
            this.client = new Client(Api_Configuration);

        }

        public bool UpdateInventory(Product prod)
        {
                var request = client.Products.Update(prod.Id, prod);
            return request.RestResponse.StatusCode == System.Net.HttpStatusCode.OK;   
        }

        public List<Product> GetInventory()
        {
            var request = client.Products.Get(new FilterOrders() { });
            return request.Data;  
        }
        
        public List<Order> GetOrders(string v)
        {
           
            var rs = client.OrderStatuses.Get();
            var rso = client.Orders.Get(new FilterOrders() {});

            List<Order> ret = new List<Order>();

            foreach (var o in rso.Data)
            {
                o.Products = client.OrdersProducts.Get(o.Id).Data;
                o.Customer = client.Customers.Get(o.CustomerId).Data;
                o.ShippingAddresses = client.OrdersShippingAddresses.Get(o.Id).Data;
                o.Shipments = client.OrdersShipments.Get(o.Id).Data;
                ret.Add(o);
            }
            return ret;
        }

        public bool UpdateShipmentStatus(string orderid, string tRACKINGNUM, string cARRIERNAME)
        {
            var getOrderShipment = client.OrdersShipments.Get(Convert.ToInt16(orderid));
            OrdersShipment shipment = getOrderShipment.Data.ElementAt(0);
            shipment.TrackingNumber = tRACKINGNUM;
            shipment.ShippingMethod = cARRIERNAME;

            var request = client.OrdersShipments.Update(Convert.ToInt16(orderid), shipment.Id, shipment);

            return request.RestResponse.StatusCode== System.Net.HttpStatusCode.OK;
        }
    }
}
