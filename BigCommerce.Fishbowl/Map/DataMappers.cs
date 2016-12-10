using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishbowlSDK;
using BigCommerce.Fishbowl.Configuration;
using BigCommerce.Fishbowl.Extensions;
using BigCommerce.Fishbowl.Models;
using BigCommerce4Net.Domain;

namespace BigCommerce.Fishbowl.Map
{
    public static class DataMappers
    {
        public static FishbowlSDK.SalesOrder MapSalesOrder(Config cfg, BCFBOrder ord, String OrderStatus)
        {
            SalesOrder salesOrder = new SalesOrder();

            var o = ord.BCOrder;

            salesOrder.CustomerName = ord.CustomerName;

            salesOrder.TotalIncludesTax = true;
            salesOrder.TotalIncludesTaxSpecified = true;

            salesOrder.CustomerPO = o.Id.ToString();

            salesOrder.Salesman = cfg.Store.OrderSettings.Salesman;
            salesOrder.Carrier = MapCarrier(cfg, o.Shipments?.ElementAt(0).ShippingMethod.ToString()??"");

            salesOrder.LocationGroup = cfg.Store.OrderSettings.LocationGroup;
            salesOrder.FOB = cfg.Store.OrderSettings.ShipTerms;
            salesOrder.Status = OrderStatus;

            salesOrder.CustomerContact = o.Customer?.Phone+" "+o.Customer?.Email.ToString();



            salesOrder.Items = MapItems(cfg, ord.BCOrder.Products).ToList();

            //salesOrder.Items.Add(AddSubTotal(salesOrder.Items.First()));

            //salesOrder.Items.Add(AddDiscountMiscSale(o.TransactionArray.Transaction.SellerDiscounts, salesOrder.Items.First()));
            /*
            if (o.giftcert_discount > 0)
            {
                salesOrder.Items.Add(AddSubTotal(salesOrder.Items.First()));
                salesOrder.Items.Add(AddGiftCertificateDiscount(o.giftcert_discount, o.giftcert_ids, salesOrder.Items.First()));
            }
*/


            double ShippingCost = 0.00;
            ShippingCost = (double) o.ShippingCostExcludingTax;
    

            salesOrder.Items.Add(AddShipping(MapCarrier(cfg, salesOrder.Carrier), "Shipping", Math.Round(ShippingCost, 2), salesOrder.Items.First()));



            salesOrder.CustomFields = MapCustomFields(cfg, ord);


            salesOrder.Ship = new ShipType
            {
                AddressField = (o.ShippingAddresses.ElementAt(0).Company ?? "").Trim() + " " + (o.ShippingAddresses.ElementAt(0).Street1 ?? "").Trim() + " " + (o.ShippingAddresses.ElementAt(0).Street2 ?? "").Trim(),
                City = o.ShippingAddresses.ElementAt(0).City,
                Country = o.ShippingAddresses.ElementAt(0).Country,
                State = o.ShippingAddresses.ElementAt(0).State,
                Zip = o.ShippingAddresses.ElementAt(0).ZipCode,
                Name = (o.ShippingAddresses.ElementAt(0).FirstName +" "+ o.ShippingAddresses.ElementAt(0).LastName).Trim()
            };

            salesOrder.BillTo = new BillType
            {
                AddressField = (o.BillingAddress.Company ?? "").Trim() + " " + (o.BillingAddress.Street1 ?? "").Trim() + " " + (o.BillingAddress.Street2 ?? "").Trim(),
                City = o.BillingAddress.City,
                Country = o.BillingAddress.Country,
                State = o.BillingAddress.State,
                Zip = o.BillingAddress.ZipCode,
                Name = (o.BillingAddress.FirstName + " " + o.BillingAddress.LastName).Trim()
            };

            return salesOrder;


        }

        private static SalesOrderItem AddSubTotal(SalesOrderItem FirstLine)
        {
            return new SalesOrderItem()
            {
                ItemType = "40",
                TaxID = FirstLine.TaxID,
                TaxRate = FirstLine.TaxRate,
                TaxCode = FirstLine.TaxCode,
                Taxable = FirstLine.Taxable,
                TaxRateSpecified = FirstLine.TaxRateSpecified
            };
        }

        private static IEnumerable<SalesOrderItem> MapItems(Config cfg, IList<OrdersProduct> items)
        {
            List<SalesOrderItem> ret = new List<SalesOrderItem>();
            foreach (OrdersProduct i in items)
            {
                ret.Add(MapSOItem(cfg, i));
            }
            return ret;
        }


        private static SalesOrderItem MapSOItem(Config cfg, OrdersProduct item)
        {

            // Add GST 
            //if (item.Taxes)
            //item.TransactionPrice.Value = Math.Round(item.TransactionPrice.Value + (item.TransactionPrice.Value * .1), 2, MidpointRounding.AwayFromZero);
            //
            String itemtype;
            decimal Price;

            if (item.IsRefunded)
            {
                itemtype = "20";
                Price = item?.RefundAmount ?? item.PriceExcludingTax;
            }
            else
            {
                itemtype = "10";
                Price = item.PriceExcludingTax;
            }

            return new SalesOrderItem
            {
                Quantity = item.Quantity,
                ProductNumber = item.Sku,
                ProductPrice = (double)Price,
                TotalPrice = item.Quantity * (double)Price,
                SOID = "-1",
                ID = "-1",
                ItemType = itemtype,
                Status = "10",
                ProductPriceSpecified = true,
                Taxable = true,
                TaxCode = cfg.Store.OrderSettings.TaxName,
                TaxRate = cfg.Store.OrderSettings.TaxRate,
                TaxRateSpecified = true,
                UOMCode = "ea"
            };
        }

        private static List<CustomField> MapCustomFields(Config cfg, BCFBOrder ord)
        {
            List<CustomField> ret = new List<CustomField>();

            ret.Add(new CustomField()
            {
                Name = "Misc Credit",
                Type = "CFT_LONG_TEXT",
                Info = ord.BCOrder.StoreCreditAmount.ToString()
            });

            return ret;
        }

        private static string MapCarrier(Config cfg, string shipping)
        {
            var dict = cfg.Store.OrderSettings.CarrierSearchNames;

            foreach (var i in dict)
            {
                bool found = shipping.ToUpper().Equals(i.Key.ToUpper());
                if (found)
                {
                    return i.Value;
                }
            }

            return cfg.Store.OrderSettings.DefaultCarrier;
        }


        private static SalesOrderItem AddShipping(string shipcode, string desc, Double shippingAmount, SalesOrderItem FirstLine)
        {
           
            //shippingAmount = Math.Round(shippingAmount * 1.1, 2);

            return new SalesOrderItem()
            {
                ItemType = "60",
                ProductNumber = shipcode,
                Description = desc,
                Quantity = 1,

                ProductPrice = shippingAmount,
                ProductPriceSpecified = true,

                TotalPrice = shippingAmount,
                TotalPriceSpecified = true,

                TaxID = FirstLine.TaxID,
                TaxRate = FirstLine.TaxRate,
                TaxCode = FirstLine.TaxCode,
                Taxable = false,
                TaxRateSpecified = FirstLine.TaxRateSpecified,
                UOMCode = "ea"
            };
        }
        public static List<BCFBOrder> MapNewOrders(Config cfg, IList<Order> orders)
        {
            var ret = new List<BCFBOrder>();

            foreach (Order o in orders)
            {
                var x = new BCFBOrder();
                x.BCOrder = o;
                x.CustomerName = MapCustomerName(cfg, o);
                ret.Add(x);
            }

            return ret;
        }
        private static string MapCustomerName(Config cfg, Order o)
        {           
            return StringExtensions.Coalesce(cfg?.Store?.OrderSettings?.DefaultCustomer, o.Customer?.FirstName+" "+o.Customer?.LastName, o.BillingAddress?.FirstName+" " + o.BillingAddress?.LastName).Trim();
        }

        public static FishbowlSDK.Customer MapCustomer(Config cfg, Order o, String customerName, CountryAndState csa)
        {
            FishbowlSDK.Customer customer = new FishbowlSDK.Customer();
            customer.CustomerID = "-1";
            customer.Status = "Normal";
            //customer.DefPaymentTerms = cfg.Store.OrderSettings.PaymentTerms;
            customer.TaxRate = null;
            customer.Name = customerName;
            customer.LastChangedUser = cfg.Store.OrderSettings.Salesman;
            customer.CreditLimit = "0";
            customer.TaxExempt = false;
            customer.TaxExemptNumber = null;
            customer.TaxExemptSpecified = true;
            customer.ActiveFlag = true;
            customer.ActiveFlagSpecified = true;
            customer.AccountingID = null;
            customer.DefaultSalesman = cfg.Store.OrderSettings.Salesman;
            customer.DefaultCarrier = cfg.Store.OrderSettings.DefaultCarrier;

            customer.JobDepth = "1";
            FishbowlSDK.Address address = new FishbowlSDK.Address();
            address.Street = o.BillingAddress.Street1+" "+ o.BillingAddress.Street2;

            address.Name = o.BillingAddress.FirstName + " " + o.BillingAddress.LastName;

            address.Attn = address.Name;

            address.Residential = true;
            address.ResidentialSpecified = true;

            address.State.Code = csa.State.CODE;
            address.State.Name = csa.State.NAME;

            address.Country.Name = csa.Country.NAME;
            address.Country.Code = csa.Country.ABBREVIATION;
            address.Country.ID = csa.Country.ID.ToString();

            address.Zip = o.BillingAddress.ZipCode;
            address.Type = "Main Office";
            address.TempAccount = null;
            address.Default = true;
            address.DefaultSpecified = true;

            address.AddressInformationList = new List<AddressInformation>()
            {
                new AddressInformation()
                {
                    Name = "Email",
                    Type = "Email",
                    Default = true,
                    DefaultSpecified = true,
                    Data = o.BillingAddress.Email
                }
            };

            customer.Addresses.Add(address);

            return customer;
        }
    }
}