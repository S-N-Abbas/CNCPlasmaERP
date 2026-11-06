using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ExpenseService> _logger;
        private readonly ICurrentUserContext _currentUserContext;

        public ExpenseService(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, ILogger<ExpenseService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserContext = currentUserContext;
        }

        public async Task<IReadOnlyList<Expense>> GetAllAsync()
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageExpenses);
            var expenses = await _unitOfWork.Expenses.GetAllAsync();
            return expenses.OrderByDescending(e => e.ExpenseDate).ToList();
        }

        public async Task<IReadOnlyList<Expense>> GetByCategoryAsync(ExpenseCategory category)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageExpenses);
            var expenses = await _unitOfWork.Expenses.FindAsync(e => e.Category == category);
            return expenses.OrderByDescending(e => e.ExpenseDate).ToList();
        }

        public async Task<Expense> CreateAsync(Expense expense)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageExpenses);
            if (expense.Amount <= 0)
                throw new BusinessValidationException("Expense amount must be greater than zero.");
            await _unitOfWork.Expenses.AddAsync(expense);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Expense recorded: {Category} — {Amount} to '{PaidTo}'", expense.Category, expense.Amount, expense.PaidTo);

            return expense;
        }
    }
}
