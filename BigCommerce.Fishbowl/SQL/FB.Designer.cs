﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BigCommerce.Fishbowl.SQL {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class FB {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal FB() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BigCommerce.Fishbowl.SQL.FB", typeof(FB).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to select * from (
        ///
        ///select product.num, COALESCE( IIF( SUM(QTYINVENTORYTOTALS.QTYONHAND) - SUM(QTYINVENTORYTOTALS.QTYALLOCATED) &lt; 0, 0, SUM(QTYINVENTORYTOTALS.QTYONHAND) - SUM(QTYINVENTORYTOTALS.QTYALLOCATED) ) ,0) as QTY
        ///from PART
        ///    join product on product.partid = part.id
        ///    left join QTYINVENTORYTOTALS on QTYINVENTORYTOTALS.PARTID = part.id
        ///where part.typeid = 10 
        ///group by 1 
        ///
        ///union all
        ///
        ///select product.num, MIN ( COALESCE( (select SUM(tag.qty)-SUM(tag.QTYCOMMITTED) from tag where partid = kp.p [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string FB_GetInventory {
            get {
                return ResourceManager.GetString("FB_GetInventory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT
        ///ship.num SNUM,
        ///so.CUSTOMERPO as CPO,
        ///LIST(distinct shipcarton.TRACKINGNUM) as TRACKINGNUM
        ///c.NAME as CarrierName
        ///from SHIP
        ///    join so on so.id = ship.SOID
        ///    join shipcarton on shipcarton.shipid = ship.ID
        ///	join CARRIER c on c.ID = SHIPCARTON.CARRIERID
        ///where ship.DATESHIPPED &gt; @dte  
        ///group by 1,2    .
        /// </summary>
        internal static string FB_GetShipmentsToUpdate {
            get {
                return ResourceManager.GetString("FB_GetShipmentsToUpdate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to select
        ///product.num as NUM
        ///from product
        ///left join CUSTOMINTEGER as checked on checked.customfieldid = (select id from customfield where tableid = 1012013120 and name = &apos;Create in Big Commerce&apos;) checked.recordid=so.id.
        /// </summary>
        internal static string GetCheckedProductNums {
            get {
                return ResourceManager.GetString("GetCheckedProductNums", resourceCulture);
            }
        }
    }
}
