using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILedgerService _ledgerService;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(IUnitOfWork unitOfWork, ILedgerService ledgerService, ILogger<CustomerService> logger)
        {
            _unitOfWork = unitOfWork;
            _ledgerService = ledgerService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Customer>> GetAllAsync() => await _unitOfWork.Customers.GetAllAsync();

        public async Task<IReadOnlyList<Customer>> GetActiveAsync() => await _unitOfWork.Customers.GetActiveAsync();

        public async Task<Customer> CreateAsync(Customer customer)
        {
            customer.CurrentBalance = 0; // set by PostOpeningBalanceAsync below if OpeningBalance is non-zero

            _unitOfWork.DetachAllTrackedEntities();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
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

        public async Task UpdateAsync(Customer customer)
        {
            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Customer {CustomerId} updated", customer.Id);
        }
    }
}
