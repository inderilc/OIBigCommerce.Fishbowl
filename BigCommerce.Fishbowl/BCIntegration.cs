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
    public class BCIntegration : IDisposable
    {
        public event LogMsg OnLog;
        public delegate void LogMsg(String msg);

        private Config cfg { get; set; }
        private FishbowlController fb { get; set; }
        private BController bc { get; set; }

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

            if (cfg.Actions.SyncInventory)
                UpdateInventory();

            if (cfg.Actions.SyncShipments)
                UpdateShipments();

            if (cfg.Actions.CreateCheckedProducts)
               CreateCheckedProducts();

            //if (cfg.Actions.SyncProductWeight)
                //UpdateProductWeight();

        }

        private void CreateCheckedProducts()
        {
            List<FishbowlSDK.Product> toBeCreated = fb.GetCheckedProducts();

            List<String> CreatedOK = bc.CreateProducts(toBeCreated);
            if(CreatedOK.Count>0)
            {
                fb.MarkCreated(CreatedOK); //should uncheck the Check Box
            }

        }

        private void UpdateShipments()
        {
            Log("Updating Shipments.");
            var shipments = fb.GetShipments(cfg.Store.SyncOrder.LastShipments);
            Log("Orders: " + shipments.Count);
            foreach (var s in shipments)
            {
                String orderid = s.CPO.ToString();
                if (!String.IsNullOrEmpty(orderid))
                {
                    bool updated = bc.UpdateShipmentStatus(orderid, s.TRACKINGNUM, s.CARRIERNAME);
                    if (updated)
                    {
                        Log($"Updated Order [{s.SONUM}] / [{s.CPO}] / [{s.ORDERNUM}] with Tracking : [{s.TRACKINGNUM}]");
                    }
                    else
                    {
                        Log($"UNABLE TO UPDATE Order [{s.SONUM}] / [{s.CPO}] / [{s.ORDERNUM}] with Tracking : [{s.TRACKINGNUM}]");
                    }
                }
                else
                {
                    Log($"Skipping Order [{s.SONUM}] Customer PO [{s.CPO}] to mark ship, possibly not a Cart Order.");
                }
            }
            cfg.Store.SyncOrder.LastShipments = DateTime.Now;
            Config.Save(cfg);
        }



        public void UpdateInventory()
        {
            Log("Updating Inventory");

            List<Product> BCProducts = bc.GetInventory();
            var fbProducts = fb.GetInventory();
            var toUpdate = new List<Product>();
            foreach (Product kvp in BCProducts)
            {
                if (fbProducts.ContainsKey(kvp.Sku))
                {
                    var dbl = fbProducts[kvp.Sku];
                    if (dbl != kvp.InventoryLevel)
                    {
                            toUpdate.Add(new Product() { Id = kvp.Id, Sku = kvp.Sku, InventoryLevel = kvp.InventoryLevel });   
                    }
                }
            }

            if (toUpdate.Count > 0)
            {
                
                Log("Updating Inventory: " + toUpdate.Count);
                foreach (Product i in toUpdate)
                {
                    var updatedGroup = bc.UpdateInventory(i);
                    if (updatedGroup)
                    {
                            Log($"Sku/Variant/Productcode: [{i.Sku}] Qty: [{i.InventoryLevel}] OK");
                     
                    }
                    else
                    {
                            Log($"Sku/Variant/Productcode: [{i.Sku}] Qty: [{i.InventoryLevel}] FAILED");
                    }
                }
            }
            else
            {
                Log("No Inventory to Update!");
            }

            Log("Inventory Update Finished");
        }


        public void DownloadOrders()
        {
            Log("Downloading Orders");
            List<Order> orders = bc.GetOrders(cfg.Store.SyncOrder.LastDownloads.ToString());

            Log("Orders Downloaded: " + orders.Count);
            if (orders.Count > 0)
            {
                List<BCFBOrder> ofOrders = DataMappers.MapNewOrders(cfg, orders);

                Log("Creating and Validating Customer Names.");
                ValidateCreateCustomers(ofOrders);
                Log("Validated Customers");

            
                Log("Validating Items in Fishbowl.");
                //ValidateItems(ofOrders);
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

        private void ValidateCreateCustomers(List<BCFBOrder> ofOrders)
        {
            foreach (var x in ofOrders)
            {
                // Does the customer exist with the first order name?
                bool IsCustomerExists = fb.CustomerExists(x.CustomerName);
                if (!IsCustomerExists)
                {
                    // Maybe it does not, so check by email address.
                    String CustomerNameByEmail = fb.FindCustomerNameByEmail(x.BCOrder.BillingAddress?.Email);
                    if (!String.IsNullOrWhiteSpace(CustomerNameByEmail))
                    {
                        x.CustomerName = CustomerNameByEmail;
                    }
                    // If it does not exist at all, try creating the customer
                    else
                    {
                        Log("Creating Customer Name: " + x.CustomerName);
                        CreateCustomer(x.CustomerName, x.BCOrder);
                        Log("Customer Created!");
                    }
                }
                // Load the Customer so we have the entire object later.
                Log("Loading Customer Fishbowl");
                var fbCustomer = fb.LoadCustomer(x.CustomerName);
                if (fbCustomer == null)
                {
                    throw new Exception(
                        "Cannot continue if a Customer Name is Missing, Or Cannot Be Loaded from Fishbowl. " +
                        x.CustomerName);
                }
            }
        }

        private void CreateCustomer(string customerName, Order BCOrder)
        {
            Log("Creating Fishbowl Customer " + customerName);
            var cas = fb.GetCountryState(BCOrder.BillingAddress.Country, BCOrder.BillingAddress.State);
            var customer = DataMappers.MapCustomer(cfg, BCOrder, customerName, cas);
            fb.CreateCustomer(customer);
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

                            var payment = fb.MakePayment(soNum, o.BCOrder.PaymentMethod.ToString(), ordertotal, cfg.Store.OrderSettings.PaymentMethodsToAccounts, (dtParsed ? dtPayment : DateTime.Now), o.FbOrder.CustomerPO.ToString()); // Use the Generated Order Total, and Payment Date
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
