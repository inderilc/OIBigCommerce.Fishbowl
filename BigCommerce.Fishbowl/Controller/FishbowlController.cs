using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using FishbowlSDK;
using Dapper;

using BigCommerce.Fishbowl.Configuration;

namespace BigCommerce.Fishbowl.Controller
{
    public class FishbowlController : IDisposable
    {

        private Config cfg;
        private FbConnection db { get; set; }
        public FishbowlSDK.Fishbowl api { get; set; }
        public FishbowlController(Config cfg)
        {
            this.cfg = cfg;
            db = InitDB();
            api = InitAPI();
        }
        private FbConnection InitDB()
        {
            String CSB = InitCSB();
            FbConnection db = new FbConnection(CSB);
            db.Open();
            return db;
        }

        private string InitCSB()
        {
            FbConnectionStringBuilder csb = new FbConnectionStringBuilder();
            csb.DataSource = cfg.FB.ServerAddress;
            csb.Database = cfg.FB.DBPath;
            csb.UserID = cfg.FB.DBUser;
            csb.Password = cfg.FB.DBPass;
            csb.Port = cfg.FB.DBPort;
            csb.ServerType = FbServerType.Default;
            return csb.ToString();
        }

        private FishbowlSDK.Fishbowl InitAPI()
        {
            var newfb = new FishbowlSDK.Fishbowl(cfg.FB.ServerAddress, cfg.FB.ServerPort, cfg.FB.FBIAKey, cfg.FB.FBIAName, cfg.FB.FBIADesc, cfg.FB.Persistent, cfg.FB.Username, cfg.FB.Password);
            return newfb;
        }

        public List<String> GetAllProducts()
        {
            return db.Query<String>("select num from product").ToList();
        }

        public bool CheckSoExists(string eBayOrderID)
        {

            String so =
                db.Query<String>("select first 1 c.ID from CUSTOMVARCHARLONG c join customfield f  on c.CUSTOMFIELDID=f.ID where (f.tableid = 1012013120 and f.name = 'Ebay Record No' and c.INFO=@cpo)", new { cpo = eBayOrderID })
                    .SingleOrDefault();
            return !(string.IsNullOrEmpty(so));
        }

        public bool SaveSalesOrder(SalesOrder fbOrder, out String SONum, out String msg, out Double OrderTotal)
        {

            try
            {
                var newSO = api.SaveSO(fbOrder, true);
                SONum = newSO.Number;
                OrderTotal = newSO.Items.Where(k => k.ItemType != "40").Select(k => k.TotalPrice).Sum() + newSO.TotalTax; // Fix Payments To Have Tax Amounts
                msg = "Created OK";
                return newSO != null;
            }
            catch (Exception ex)
            {
                SONum = "";
                msg = ex.Message + " [order id: " + fbOrder.CustomerPO + "]";
                OrderTotal = 0;
                return false;
            }

        }
        public String MakePayment(string sonum, string Method, double Total, Dictionary<string, string> methods, DateTime DatePayment, string userName)
        {
            MakePaymentRqType rq = new MakePaymentRqType();

            rq.Payment = new Payment()
            {
                SalesOrderNumber = sonum,
                Amount = Total.ToString(),
                PaymentMethod = Method,
                PaymentDate = DatePayment.ToString("o")

            };

            if (methods.ContainsKey(Method))
            {
                rq.Payment.DepositAccountName = methods[Method];
            }

            MakePaymentRsType rs = api.sendAnyRequest(rq) as MakePaymentRsType;

            if (rs.statusCode == "1000")
            {
                return $"Payment Applied. [user id: {userName}]";
            }
            else
            {
                return $"Payment NOT Applied. [user id:{userName}]. {rs.statusMessage}";
            }
        }



        public void Dispose()
        {
            if (api != null)
                api.Dispose();

            if (db != null)
                db.Dispose();
        }
    }
}
