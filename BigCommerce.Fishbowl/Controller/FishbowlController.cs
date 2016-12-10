﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using FishbowlSDK;
using Dapper;

using BigCommerce.Fishbowl.Configuration;
using BigCommerce.Fishbowl.Models;
using BigCommerce.Fishbowl.Extensions;

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

        public bool CheckSoExists(string OrderID)
        {

            String so =
                db.Query<String>("select first 1 num from so where customerpo = @cpo", new { cpo = OrderID })
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
        public void CreateCustomer(Customer customer)
        {
            api.SaveCustomer(customer, true);
        }

        public object LoadCustomer(string customerName)
        {
            return api.GetCustomer(customerName);
        }
        public string FindCustomerNameByEmail(object email)
        {
            return db.Query<String>("select first 1 customer.name from CONTACT join customer on customer.accountid = contact.ACCOUNTID where contact.DATUS = @eml and contact.typeid = 60", new { eml = email }).SingleOrDefault();
        }
        public bool CustomerExists(object customerName)
        {
            var text = db.Query<String>("select name from customer where name = @c", new { c = customerName }).SingleOrDefault();
            return customerName.Equals(text);
        }

        public CountryAndState GetCountryState(string Country, string State)
        {
            CountryAndState cas = new CountryAndState();

            Countryconst ct;
            Stateconst st;

            /// Get the country
            ct = db.Query<Countryconst>("select first 1 * from countryconst where UPPER(abbreviation) containing UPPER(@abb) or UPPER(name) containing UPPER(@n) ", new { n = Country, abb = Country.Truncate(10) }).FirstOrDefault();

            // If we have no country, lookup just by state
            if (ct == null || ct.ID == null)
            {
                st = db.Query<Stateconst>("select first 1 * from stateconst where UPPER(name) containing UPPER(@st) or UPPER(code) containing UPPER(@abb)  ", new { st = State, abb = State.Truncate(21) }).FirstOrDefault();
            }
            else // If we have a country, include that in the lookup
            {
                st = db.Query<Stateconst>("select first 1 * from stateconst where UPPER(name) containing UPPER(@st) or UPPER(code) containing UPPER(@abb) and countryconstid = @cid ", new { st = State, abb = State.Truncate(21), cid = ct.ID }).FirstOrDefault();
            }

            // If we have a state and no country
            if (st != null && ct == null)
            {
                // Lookup the country
                ct = db.Query<Countryconst>("select first 1 * from countryconst where id = @cid", new { cid = st.COUNTRYCONSTID }).FirstOrDefault();
            }

            if (st == null || ct == null)
            {
                throw new Exception("Cant find Country and Or State. [" + Country + "] [" + State + "] ");
            }

            cas.State = st;
            cas.Country = ct;

            return cas;
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
