using System;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace CNCPlasmaERP.Data
{
    internal class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfUnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync() => _transaction.CommitAsync();

        public Task RollbackAsync() => _transaction.RollbackAsync();

        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
