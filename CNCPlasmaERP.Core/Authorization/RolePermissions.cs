using System;
using System.Collections.Generic;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Authorization
{
    /// <summary>
    /// Fixed role -&gt; permission matrix for the system's 4 roles. Deliberately a
    /// simple static lookup rather than a database-driven RBAC table, since the
    /// role set itself is fixed by business requirement. Adjust the sets below if
    /// the shop's access rules change — that's the only place this ever needs editing.
    ///
    ///   Admin    — everything, including Users and Settings.
    ///   Manager  — day-to-day operations and money (jobs, stock, customers,
    ///              suppliers, expenses, invoices/payments, reports) but not
    ///              user management or system settings.
    ///   Operator — jobs only (creating/updating jobs, recording cutting and
    ///              scrap) — no financial visibility.
    ///   Cashier  — invoices, payments, and the customer ledger — no stock or
    ///              job editing.
    /// </summary>
    public static class RolePermissions
    {
        private static readonly Dictionary<UserRole, HashSet<Permission>> Map = new()
        {
            [UserRole.Admin] = new HashSet<Permission>((Permission[])Enum.GetValues(typeof(Permission))),

            [UserRole.Manager] = new HashSet<Permission>
            {
                Permission.ManageJobs,
                Permission.ManageStock,
                Permission.ManageCustomers,
                Permission.ManageSuppliers,
                Permission.ManageExpenses,
                Permission.ManageInvoicesPayments,
                Permission.ViewFinancialReports
            },

            [UserRole.Operator] = new HashSet<Permission>
            {
                Permission.ManageJobs
            },

            [UserRole.Cashier] = new HashSet<Permission>
            {
                Permission.ManageInvoicesPayments,
                Permission.ManageCustomers,
                Permission.ViewFinancialReports
            }
        };

        public static bool Has(UserRole role, Permission permission) =>
            Map.TryGetValue(role, out var permissions) && permissions.Contains(permission);
    }
}
