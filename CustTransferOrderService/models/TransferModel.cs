using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustTransferOrderService.models
{
    internal class TransferModel
    {
        public string ItemCode { get; set; }                  // nvarchar(50)
        public string SourceLocation { get; set; }            // nvarchar(50)
        public string DestinationLocation { get; set; }       // nvarchar(50)
        public string TransferNumber { get; set; }            // nvarchar(50)
        public string LineNumber { get; set; }                // nvarchar(50)
        public DateTime TransferDate { get; set; }            // datetime
        public DateTime ExpectedArrivalDate { get; set; }     // datetime
        public decimal TransferQty { get; set; }              // numeric(32, 16)
        public decimal QuantityToShip { get; set; }           // numeric(32, 16)
        public decimal QuantityToReceive { get; set; }        // numeric(32, 16)
    }
}
