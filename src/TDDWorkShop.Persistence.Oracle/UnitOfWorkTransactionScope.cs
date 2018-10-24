using System;
using System.Transactions;

namespace TDDWorkShop.Persistence.Oracle
{
    public class UnitOfWorkTransactionScope : IUnitOfWork, IDisposable
    {
        private TransactionScope _transactionScope;

        public void Start()
        {
            if (_transactionScope != null)
                throw new InvalidOperationException("Transaction scope already active");
            _transactionScope = new TransactionScope();

        }

        public void Complete()
        {
            if (_transactionScope == null)
                throw new InvalidOperationException("Transaction scope has not been started");
            _transactionScope.Complete();
        }

        public void Dispose()
        {
            _transactionScope?.Dispose();
        }
    }
}
