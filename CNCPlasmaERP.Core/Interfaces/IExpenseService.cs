using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface IExpenseService
    {
        /// <summary>All expenses, newest first.</summary>
        Task<IReadOnlyList<Expense>> GetAllAsync();

        /// <summary>Expenses in one category, newest first — used by the Wages screen (Category = Wages).</summary>
        Task<IReadOnlyList<Expense>> GetByCategoryAsync(ExpenseCategory category);

        Task<Expense> CreateAsync(Expense expense);
    }
}
