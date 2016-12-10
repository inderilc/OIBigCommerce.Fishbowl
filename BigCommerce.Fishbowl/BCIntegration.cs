﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using BigCommerce.Fishbowl.Controller;
using BigCommerce.Fishbowl.Configuration;
using BigCommerce.Fishbowl.Models;
using BigCommerce.Fishbowl.Map;

using BigCommerce4Net.Domain;

namespace BigCommerce.Fishbowl
{
    class BCIntegration : IDisposable
    {
        public event LogMsg OnLog;
        public delegate void LogMsg(String msg);

        private Config cfg { get; set; }
        private FishbowlController fb { get; set; }
        private BController bc { get; set; }

        public static void Main(string[] args)
        {
            BCIntegration obj = new BCIntegration(Config.Load());
            obj.Run();
        }
       
        public BCIntegration(Config cfg)
        {
            this.cfg = cfg;
            if (bc == null)
            {
                bc = new BController(cfg);
            }
            if (fb == null)
            {
                fb = new FishbowlController(cfg);
            }
        }
        public void Run()
        {

            Log("Starting Integration");
            InitConnections();
            Log("Ready");
            if (cfg.Actions.SyncOrders)
                DownloadOrders();

            //if (cfg.Actions.SyncInventory)
                //UpdateInventory();

            //if (cfg.Actions.SyncShipments)
                //UpdateShipments();

            //if (cfg.Actions.SyncProductPrice)
               // UpdateProductPrice();

            //if (cfg.Actions.SyncProductWeight)
                //UpdateProductWeight();

        }
        public void DownloadOrders()
        {
            Log("Downloading Orders");
            List<Order> orders = bc.GetOrders(cfg.Store.SyncOrder.LastDownloads.ToString());

            Log("Orders Downloaded: " + orders.Count);
            if (orders.Count > 0)
            {
                List<BCFBOrder> ofOrders = DataMappers.MapNewOrders(cfg, orders);

                Log("Validating Items in Fishbowl.");
                ValidateItems(ofOrders);
                Log("Items Validated");
                
                Log("Creating Sales Orders Data.");
                ValidateOrder(ofOrders, "20");
                Log("Finished Creating Sales Order Data.");

                Log("Validate Carriers");
                ValidateCarriers(ofOrders);
                Log("Carriers Validated");

                //Log("Kit Items");
                // ValidateKits(ofOrders);
                // Log("Finished Kits.");

                var ret = CreateSalesOrders(ofOrders);

                //Log("Result: " + String.Join(Environment.NewLine, ret));
                cfg.Store.SyncOrder.LastDownloads = DateTime.Now;
                Config.Save(cfg);
                Log("Downloading Orders Finished");
            }

        }
        private List<String> CreateSalesOrders(List<BCFBOrder> ofOrders)
        {
            var ret = new List<String>();

            foreach (var o in ofOrders)
            {
                String soNum;

                bool soExists = fb.CheckSoExists(o.BCOrder.Id.ToString());

                if (!soExists)
                {
                    String msg = "";
                    Double ordertotal;
                    var result = fb.SaveSalesOrder(o.FbOrder, out soNum, out msg, out ordertotal);

                    //xc.UpdateXC2FBDownloaded(o.XCOrder.orderid, soNum);

                    try
                    {
                        if (result && o.FbOrder.Status.Equals("20")) // Only apply payments on Issued Orders.
                        {
                            DateTime dtPayment;
                            bool dtParsed = DateTime.TryParse(o.BCOrder.DateCreated.ToString(), out dtPayment);

                            var payment = fb.MakePayment(soNum, o.BCOrder.PaymentMethod.ToString(), ordertotal, cfg.Store.SyncOrder.PaymentMethodsToAccounts, (dtParsed ? dtPayment : DateTime.Now), o.FbOrder.CustomerPO.ToString()); // Use the Generated Order Total, and Payment Date
                            ret.Add(payment);
                        }
                        else
                        {
                            ret.Add(msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        ret.Add("Error With Payment. " + ex.Message);
                    }
                }
                else
                {
                    ret.Add("SO Exists.");
                }
                Config.Save(cfg);
            }

            return ret;
        }
        private void ValidateItems(List<BCFBOrder> ofOrders)
        {
            var fbProds = fb.GetAllProducts();
            foreach (var i in ofOrders)
            {    
                var orderProducts = i.BCOrder.Products;
                List<String> prods = new List<String>();

                foreach (OrdersProduct op in orderProducts)
                {
                    prods.Add(op.Sku);
                }

                var except = prods.Except(fbProds);
                if (except.Any())
                {
                    throw new Exception($"Products Not Found on Order [{i.BCOrder.Id}] Please Create Them: " + String.Join(",", except));
                }
            }
        }
        private void ValidateOrder(List<BCFBOrder> ofOrders, String OrderStatus)
        {
            foreach (var o in ofOrders)
            {
                o.FbOrder = DataMappers.MapSalesOrder(cfg, o, OrderStatus);
            }
        }
        private void ValidateCarriers(List<BCFBOrder> ofOrders)
        {
            // Do nothing.
        }






        public void Log(String msg)
        {
            if (OnLog != null)
            {
                OnLog(msg);
            }
        }

        private void InitConnections()
        {
            if (fb == null)
            {
                Log("Connecting to Fishbowl");
                fb = new FishbowlController(cfg);
            }

            if (bc == null)
            {
                Log("Connecting to BigCommerce Store");
                bc = new BController(cfg);
            }
        }

        private void LogException(Exception ex)
        {
            String msg = ex.Message;
            Log(msg);
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "exception.txt", ex.ToString() + Environment.NewLine);
        }


        public void Dispose()
        {
            if (fb != null)
                fb.Dispose();

        }
    }
}
