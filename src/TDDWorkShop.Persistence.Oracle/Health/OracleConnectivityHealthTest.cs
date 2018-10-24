using System;

using Coolblue.Utilities.ApplicationHealth;
using Coolblue.Utilities.MonitoringEvents;

using TDDWorkShop.Persistence.Oracle.Infrastructure;
using System.Data;
using Coolblue.Utilities.Data.Timing;
using Oracle.ManagedDataAccess.Client;

namespace TDDWorkShop.Persistence.Oracle.Health
{
    internal class OracleConnectivityHealthTest : HealthTest
    {
        protected ITimingDbConnectionFactory ConnectionFactory;
        protected override string HealthTestName => "Oracle connectivity";
        public MonitoringEvents MonitoringEvents { get; set; }        

        public OracleConnectivityHealthTest(ITimingDbConnectionFactory connectionFactory)
        {
            ConnectionFactory = connectionFactory;
        }

        protected override TestResult DetermineHealth()
        {
            try
            {
                OpenConnection();
                return TestResult.Pass;
            }
            catch (OracleException ex)
            {
                MonitoringEvents.TestingOracleConnectivity(ex);
                return TestResult.Fail;
            }
            catch(Exception ex)
            {
                MonitoringEvents.TestingOracleConnectivity(ex);
                return TestResult.Fail;
            }
        }

        private void OpenConnection()
        {
            using (var connection = ConnectionFactory.CreateConnection())
            {
                connection.Open();
            }
        }

    }
}