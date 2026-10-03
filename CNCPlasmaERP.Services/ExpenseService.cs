using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(IUnitOfWork unitOfWork, ILogger<ExpenseService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Expense>> GetAllAsync()
        {
            var expenses = await _unitOfWork.Expenses.GetAllAsync();
            return expenses.OrderByDescending(e => e.ExpenseDate).ToList();
        }

        public async Task<IReadOnlyList<Expense>> GetByCategoryAsync(ExpenseCategory category)
        {
            var expenses = await _unitOfWork.Expenses.FindAsync(e => e.Category == category);
            return expenses.OrderByDescending(e => e.ExpenseDate).ToList();
        }

        public async Task<Expense> CreateAsync(Expense expense)
        {
            await _unitOfWork.Expenses.AddAsync(expense);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Expense recorded: {Category} — {Amount} to '{PaidTo}'", expense.Category, expense.Amount, expense.PaidTo);

            return expense;
        }
    }
}
