namespace CNCPlasmaERP.Core.Exceptions
{
    public class DuplicateMaterialException : DomainException
    {
        public DuplicateMaterialException(string name, string gauge)
            : base($"A material named '{name}' with gauge '{gauge}' already exists.")
        {
        }
    }
}
