namespace CNCPlasmaERP.Core.Enums
{
    /// <summary>Base material type for a sheet.</summary>
    public enum MaterialType
    {
        MS,         // Mild Steel
        SS,         // Stainless Steel
        Aluminum,
        Brass,
        Copper,
        Other
    }

    /// <summary>Fixed system roles. Kept as an enum since the role set is small
    /// and fixed by business requirement; permissions are checked against this
    /// in the Services layer.</summary>
    public enum UserRole
    {
        Admin,
        Manager,
        Operator,
        Cashier
    }

    /// <summary>Lifecycle of a work order (Job).</summary>
    public enum JobStatus
    {
        Pending,
        InProgress,
        Completed,
        Invoiced,
        Cancelled
    }

    public enum InvoiceStatus
    {
        Unpaid,
        PartiallyPaid,
        Paid,
        Cancelled
    }

    public enum PaymentMethod
    {
        Cash,
        Bank,
        Cheque,
        Online
    }

    /// <summary>Type of movement recorded against a customer's running account.</summary>
    public enum TransactionType
    {
        Invoice,
        Payment,
        Advance,
        Adjustment,
        OpeningBalance
    }

    /// <summary>Category of a printed invoice line item.</summary>
    public enum InvoiceItemType
    {
        Material,
        Service,
        Carriage,
        Other
    }

    /// <summary>A quotation is record-only; status is informational and never
    /// triggers stock or ledger movement.</summary>
    public enum QuotationStatus
    {
        Draft,
        Sent,
        Converted,
        Expired
    }

    /// <summary>Wages are recorded as an Expense under the Wages category rather
    /// than a dedicated table, per business requirement to keep it simple.</summary>
    public enum ExpenseCategory
    {
        Maintenance,
        Electricity,
        Rent,
        Consumables,
        Wages,
        Fuel,
        Other
    }
}
