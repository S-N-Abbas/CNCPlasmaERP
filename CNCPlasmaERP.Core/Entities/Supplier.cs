using System.Collections.Generic;
using CNCPlasmaERP.Core.Common;

namespace CNCPlasmaERP.Core.Entities
{
    public class Supplier : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<StockPurchase> Purchases { get; set; } = new List<StockPurchase>();
    }
}
