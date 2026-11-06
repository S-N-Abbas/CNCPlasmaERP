using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILedgerService _ledgerService;
        private readonly ILogger<CustomerService> _logger;
        private readonly ICurrentUserContext _currentUserContext;

        public CustomerService(IUnitOfWork unitOfWork, ILedgerService ledgerService, ICurrentUserContext currentUserContext, ILogger<CustomerService> logger)
        {
            _unitOfWork = unitOfWork;
            _ledgerService = ledgerService;
            _currentUserContext = currentUserContext;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Customer>> GetAllAsync()
        {
            var customers = await _unitOfWork.Customers.GetAllAsync();
            return customers.OrderBy(c => c.Name).ToList();
        }

        public async Task<IReadOnlyList<Customer>> GetActiveAsync() => await _unitOfWork.Customers.GetActiveAsync();

        public async Task<Customer> CreateAsync(Customer customer)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageCustomers);
            if (string.IsNullOrWhiteSpace(customer.Name) || customer.CreditLimit < 0)
                throw new BusinessValidationException("Customer name is required and credit limit cannot be negative.");
            customer.CurrentBalance = 0; // set by PostOpeningBalanceAsync below if OpeningBalance is non-zero

            _unitOfWork.DetachAllTrackedEntities();

            await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();

                await _ledgerService.PostOpeningBalanceAsync(customer);

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _unitOfWork.DetachAllTrackedEntities();
                _logger.LogError(ex, "Failed to create customer '{Name}'; transaction rolled back", customer.Name);
                throw;
            }

            _logger.LogInformation(
                "Customer '{Name}' created with opening balance {OpeningBalance}", customer.Name, customer.OpeningBalance);

            return customer;
        }

        /// <summary>
        /// Edits contact details, credit limit and active status only.
        /// OpeningBalance and CurrentBalance are deliberately never copied: the
        /// balance must always equal the sum of the customer's ledger entries,
        /// and the incoming object may be a stale grid row. Balances change only
        /// through LedgerService.
        /// </summary>
        public async Task UpdateAsync(Customer customer)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageCustomers);
            if (string.IsNullOrWhiteSpace(customer.Name) || customer.CreditLimit < 0)
                throw new BusinessValidationException("Customer name is required and credit limit cannot be negative.");
            var existing = await _unitOfWork.Customers.GetByIdAsync(customer.Id)
                ?? throw new KeyNotFoundException($"Customer {customer.Id} not found.");

            // A deactivated customer vanishes from the Payments and Jobs
            // dropdowns, so one who still owes money (or holds an advance)
            // would be unreachable for settling it.
            if (existing.IsActive && !customer.IsActive && existing.CurrentBalance != 0)
            {
                throw new CannotDeactivateException(
                    $"Cannot deactivate '{existing.Name}' while their account balance is {existing.CurrentBalance:N2}. " +
                    "Settle it with a payment or adjustment first.");
            }

            existing.Name = customer.Name;
            existing.ContactPerson = customer.ContactPerson;
            existing.Phone = customer.Phone;
            existing.Email = customer.Email;
            existing.Address = customer.Address;
            existing.CreditLimit = customer.CreditLimit;
            existing.IsActive = customer.IsActive;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Customer {CustomerId} updated (active: {IsActive})", existing.Id, existing.IsActive);
        }
    }
}
