using System;
using System.Collections.Generic;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.UI.Models
{
    public class StockPrintData
    {
        public AppSettings Settings { get; init; } = null!;
        public IReadOnlyList<Material> Materials { get; init; } = new List<Material>();
        public DateTime PrintedOn { get; init; } = DateTime.Now;
    }
}
