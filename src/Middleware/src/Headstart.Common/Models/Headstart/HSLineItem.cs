using System.Collections.Generic;
using OrderCloud.SDK;

namespace Headstart.Common.Models
{
    public class HSLineItem : LineItem<LineItemXp, HSLineItemProduct, LineItemVariant, HSAddressBuyer, HSAddressSupplier>
    {
    }

    public class HSPartialLineItem : PartialLineItem<LineItemXp, HSLineItemProduct, LineItemVariant, HSAddressBuyer, HSAddressSupplier>
    {
    }

    public class LineItemXp
    {
        public Dictionary<LineItemStatus, int> StatusByQuantity { get; set; }

        public string ImageUrl { get; set; }

        // needed for Line Item Detail Report
        public string ShipMethod { get; set; }

        public string SupplierComments { get; set; } // xp.Comments is already being used as ship comments for SEB
    }
}
