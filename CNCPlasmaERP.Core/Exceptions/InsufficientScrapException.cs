namespace CNCPlasmaERP.Core.Exceptions
{
    public class InsufficientScrapException : DomainException
    {
        public int MaterialId { get; }
        public decimal RequestedKg { get; }
        public decimal AvailableKg { get; }

        public InsufficientScrapException(int materialId, decimal requestedKg, decimal availableKg)
            : base($"Insufficient scrap balance: requested {requestedKg:N3} Kg but only {availableKg:N3} Kg available.")
        {
            MaterialId = materialId;
            RequestedKg = requestedKg;
            AvailableKg = availableKg;
        }
    }
}
