using System;

using Coolblue.Utilities.Data.Timing;

using TDDWorkShop.Persistence.Oracle.Infrastructure;


using Oracle.ManagedDataAccess.Client;
using Coolblue.Utilities.Resilience.Oracle;

namespace TDDWorkShop.Persistence.Oracle
{
    public class RequestDataStore : OracleDataStoreBase, IRequestRegistery
    {
        private const string requestStoreQueryName = "store_request";

        public RequestDataStore(ITimingDbConnection connection, OracleResiliencePolicy resiliencePolicy) : base(connection, resiliencePolicy)
        { }


        public bool RequestExists(RequestId requestId)
        {
            bool ExecuteQuery()
            {
                return Connection.ExecuteScalar<bool>(
                    queryDescription: nameof(RequestExists),
                    sql: "select " +
                         "(case " +
                         "when exists(select null from DEVVANESSA.VAN_REQUEST where REQUESTUUID = :REQUESTID) " +
                         "then 1 else 0 " +
                         "end) " +
                         "from dual",
                    param: new
                    {
                        requestId = requestId.ToString(),
                    }
                );
            }

            return ExecuteWithPolicy(ExecuteQuery);
        }

        public void StoreRequest(RequestId requestId)
        {
            void ExecuteQuery()
            {
                Connection.Execute(
                    queryDescription: nameof(StoreRequest),
                    sql: "insert into DEVVANESSA.VAN_REQUEST " +
                         "(REQUESTDATETIME, REQUESTUUID, CLIENTID) " +
                         "values (sysdate, :REQUESTID, :CLIENTID)",
                    param: new
                    {
                        requestId = requestId.ToString(),
                        clientId = "People",
                    }
                );
            };

            try
            {
                ExecuteWithPolicy(ExecuteQuery);
            }
            catch (Exception ex)
            {
                var persistenceException = ex as PersistenceException;
                if (IsDuplicateException(persistenceException))
                {
                    throw new DuplicateRequestException(requestId, ex);
                }
                throw ex;
            }
        }

        private static bool IsDuplicateException(PersistenceException persistenceException)
        {
            Func<PersistenceException, bool> innerIsDuplicate = ex =>
            {
                if (persistenceException.InnerException == null) return false;

                var oracleEx = ex.InnerException as OracleException;
                if (oracleEx == null) return false;
                if (oracleEx.Message != null && oracleEx.Message.StartsWith("ORA-00001")) return true;
                return false;
            };

            return persistenceException != null && innerIsDuplicate(persistenceException);
        }

    }
}
