using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigCommerce4Net;
using BigCommerce4Net.Api;
using BigCommerce.Fishbowl.Configuration;
using BigCommerce4Net.Domain;
using FishbowlSDK;

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

        public bool UpdateInventory(BigCommerce4Net.Domain.Product prod)
        {
                var request = client.Products.Update(prod.Id, prod);
            return request.RestResponse.StatusCode == System.Net.HttpStatusCode.OK;   
        }

        public List<BigCommerce4Net.Domain.Product> GetInventory()
        {
            var request = client.Products.Get(new FilterOrders() { });
            return request.Data;  
        }
        
        public List<Order> GetOrders(string v)
        {

            

            var status = client.OrderStatuses.Get();
            Dictionary<String, bool> dict = cfg.Store.DownloadOrderTypes;
            DateTime fromDte = cfg.Store.SyncOrder.LastDownloads;

            List<Order> allOrder = new List<Order>();
            
            foreach (var s in status.Data)
            {
                if (dict?[s.Name]==true)
                {
                    var orderRes = client.Orders.Get(new FilterOrders() {StatusId=s.Id,MinimumDateCreated=fromDte, MaximumDateCreated=DateTime.Now});
                    if (orderRes.RestResponse.StatusCode.Equals(System.Net.HttpStatusCode.OK))
                    {
                        allOrder.AddRange(orderRes.Data);
                    }
                }
            }

            //var rso = client.Orders.Get(new FilterOrders() {});

            List<Order> ret = new List<Order>();
            
            foreach (var o in allOrder)
            {
                o.Products = client.OrdersProducts.Get(o.Id).Data;
                o.Customer = client.Customers.Get(o.CustomerId).Data;
                o.ShippingAddresses = client.OrdersShippingAddresses.Get(o.Id).Data;
                o.Shipments = client.OrdersShipments.Get(o.Id).Data;
                ret.Add(o);
            }
            return ret;
        }

        public List<String> CreateProducts(List<FishbowlSDK.Product> toBeCreated)
        {
            List<String> ret = new List<String>();

            foreach (var p in toBeCreated)
            {
                BigCommerce4Net.Domain.Product bcPro = MapFBtoBC(p);
                var request = client.Products.Create(bcPro);
                

                if (request.RestResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ret.Add(p.Num);
                }
                ret.Add(p.Num);
            }
            
            return ret;
        }

        public BigCommerce4Net.Domain.Product MapFBtoBC(FishbowlSDK.Product product)
        {

            var st = client.Products.Get(new FilterProducts());
            

            List<int> catgs = new List<int>();
            catgs.Add(18);

            BigCommerce4Net.Domain.Product ret = new BigCommerce4Net.Domain.Product();


            ret = st.Data.ElementAt(0);

            ret.DateCreated = DateTime.Now;
            ret.DateModified = DateTime.Now;
            ret.EventDateStart = DateTime.Now;
            ret.EventDateEnd = DateTime.Now.AddYears(1);
            ret.DateLastImported = DateTime.Now;

            ret.PreorderReleaseDate = DateTime.Now;
            ret.PreorderMessage = "Now Available for Pre-Order";

            ret.Description = product.Details;
            ret.Sku = product.Num;
            ret.Name = product.Description;
            ret.Price = Convert.ToDecimal(product.Price);
            ret.RelatedProducts = "None";
            ret.Categories = catgs;
            ret.Warranty = "None";
            ret.PageTitle = product.Description;
            ret.Upc = product.UPC;

            ret.LayoutFile = ""; //??? need to check what this is
            ret.OpenGraphTitle = ""; //??? need to check what this is
            ret.OpenGraphDescription = "";//??? need to check what this is
            ret.PriceHiddenLabel = "";

            ret.MyobAssetAccount = "";
            ret.MyobExpenseAccount = "";
            ret.MyobIncomeAccount = "";
            ret.PeachtreeGlAccount = "";
            ret.TaxClassId = 0;
            
            List<ProductsImage> img = new List<ProductsImage>();

            ret.Images = null;
            ret.ResourceImages = null;
            ret.DiscountRules = null;
            ret.ResourceDiscountRules = null;
            ret.ResourceRules = null;
            ret.Rules = null;
            ret.ConfigurableFields = null;
            ret.ResourceConfigurableFields = null;
            ret.CustomFields = null;
            ret.ResourceCustomFields = null;
            ret.ResourceVideos = null;
            ret.Videos = null;
            ret.Skus = null;
            ret.ResourceSkus = null;
            ret.OptionSets = null;
            ret.OptionSetId = null;
            ret.ResourceOptionSet = null;
            ret.Options = null;
            ret.ResourceOptions = null;
            

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
