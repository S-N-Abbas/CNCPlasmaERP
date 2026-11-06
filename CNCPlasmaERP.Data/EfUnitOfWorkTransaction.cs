using System;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace CNCPlasmaERP.Data
{
    internal class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;
        private readonly bool _ownsTransaction;

        public EfUnitOfWorkTransaction(IDbContextTransaction transaction, bool ownsTransaction = true)
        {
            _transaction = transaction;
            _ownsTransaction = ownsTransaction;
        }

        public Task CommitAsync() => _ownsTransaction ? _transaction.CommitAsync() : Task.CompletedTask;

        public Task RollbackAsync() => _ownsTransaction ? _transaction.RollbackAsync() : Task.CompletedTask;

        public ValueTask DisposeAsync() => _ownsTransaction ? _transaction.DisposeAsync() : ValueTask.CompletedTask;
    }
}
