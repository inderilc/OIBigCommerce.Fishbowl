using System;
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
                //ValidateItems(ofOrders);
                Log("Items Validated");


                Log("Creating Sales Orders Data.");
               // ValidateOrder(ofOrders, "20");
                Log("Finished Creating Sales Order Data.");

                Log("Validate Carriers");
                //ValidateCarriers(ofOrders);
                Log("Carriers Validated");

                //Log("Kit Items");
                // ValidateKits(ofOrders);
                // Log("Finished Kits.");

                //var ret = CreateSalesOrders(ofOrders);

                //Log("Result: " + String.Join(Environment.NewLine, ret));
                cfg.Store.SyncOrder.LastDownloads = DateTime.Now;
                Config.Save(cfg);
                Log("Downloading Orders Finished");
            }

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
