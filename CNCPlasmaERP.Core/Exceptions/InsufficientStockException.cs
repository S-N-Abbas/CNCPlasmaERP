namespace CNCPlasmaERP.Core.Exceptions
{
    public class InsufficientStockException : DomainException
    {
        public int MaterialId { get; }
        public decimal Requested { get; }
        public decimal Available { get; }

        public InsufficientStockException(int materialId, decimal requested, decimal available)
            : base($"Insufficient stock: requested {requested:N3} but only {available:N3} available.")
        {
            MaterialId = materialId;
            Requested = requested;
            Available = available;
        }
    }
}
