using System;
using Polly.CircuitBreaker;

using Coolblue.Utilities.Data.Timing;
using Coolblue.Utilities.Resilience.Oracle;

namespace TDDWorkShop.Persistence.Oracle
{
    public class OracleDataStoreBase
    {
        private readonly OracleResiliencePolicy resiliencePolicy;

        protected ITimingDbConnection Connection { get; }

        protected OracleDataStoreBase(ITimingDbConnection connection, OracleResiliencePolicy resiliencePolicy)
        {
            resiliencePolicy = resiliencePolicy ?? throw new ArgumentNullException(nameof(resiliencePolicy));

            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.resiliencePolicy = resiliencePolicy;
        }

        protected T ExecuteWithPolicy<T>(Func<T> func)
        {
            var oraclePolicy = resiliencePolicy.Value;

            try
            {
                return oraclePolicy.Execute(func);
            }
            catch (BrokenCircuitException ex)
            {
                throw new PersistenceUnavailableException("Oracle circuit breaker is currently open.", ex);
            }
            catch (Exception ex)
            {
                throw new PersistenceException("Exception occurred while executing Oracle query.", ex);
            }
        }

        protected void ExecuteWithPolicy(Action action)
        {
            ExecuteWithPolicy(() =>
            {
                action();
                return 1;
            });

        }
    }
}