namespace CNCPlasmaERP.Core.Authorization
{
    /// <summary>Coarse-grained feature areas the UI checks access against before enabling a screen or action.</summary>
    public enum Permission
    {
        ManageUsers,
        ManageSettings,
        ManageJobs,
        ManageStock,
        ManageCustomers,
        ManageSuppliers,
        ManageExpenses,
        ManageInvoicesPayments,
        ViewFinancialReports
    }
}
