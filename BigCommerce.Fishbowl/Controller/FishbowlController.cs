using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using FishbowlSDK;

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

       
        public void Dispose()
        {
            if (api != null)
                api.Dispose();

            if (db != null)
                db.Dispose();
        }
    }
}
