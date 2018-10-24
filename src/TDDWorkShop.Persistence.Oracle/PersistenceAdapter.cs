using System;
using System.Collections.Generic;
using System.Data;

using SimpleInjector;
using SimpleInjector.Lifestyles;

using Coolblue.Utilities.ApplicationHealth;
using Coolblue.Utilities.MonitoringEvents;
using Coolblue.Utilities.Data.Timing;


using TDDWorkShop.Persistence.Oracle.Health;
using Oracle.ManagedDataAccess.Client;
using Coolblue.Utilities.Resilience.Oracle;

namespace TDDWorkShop.Persistence.Oracle
{
    public class PersistenceAdapter
    {

        private readonly PersistenceAdapterSettings _settings;
        private MonitoringEvents _monitoringEvent;
        private bool _isRegistered;
        private ITimingDbConnectionFactory _connectionFactory;

        public PersistenceAdapter(PersistenceAdapterSettings settings, MonitoringEvents monitoringEvents)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _monitoringEvent = monitoringEvents;
        }

        public void Register(Container container)
        {
            if (_isRegistered)
                throw new InvalidOperationException("Persistence adapter was already registered with a container.");
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            RegisterResilience(container);
            RegisterConnection(container);
            RegisterRepositories(container);

            _isRegistered = true;
        }

        private static void RegisterRepositories(Container container)
        {
            container.Register<IRequestRegistery, RequestDataStore>();

        }

        private void RegisterConnection(Container container)
        {
            Func<IDbConnection> connectionFactory = () => new OracleConnection(_settings.OracleConnectionString);
            _connectionFactory = new TimingDbConnectionFactory(connectionFactory, "Oracle") { MonitoringEvents = _monitoringEvent };
            container.RegisterSingleton<ITimingDbConnectionFactory>(_connectionFactory);
            container.Register(() => container.GetInstance<ITimingDbConnectionFactory>().CreateConnection(), new AsyncScopedLifestyle());
            container.Register<IUnitOfWork>(() => new UnitOfWorkTransactionScope());
        }

        private void RegisterResilience(Container container)
        {
            container.RegisterSingleton<OracleResiliencePolicy>(() => new OracleResiliencePolicy(_settings.OracleResilience));
        }

        public IEnumerable<IHealthTest> GetHealthTests()
        {
            if (!_isRegistered)
                throw new InvalidOperationException("Call Register before retrieving the health tests.");

            return new[]
            {
                new OracleConnectivityHealthTest(_connectionFactory)
                {
                    MonitoringEvents = _monitoringEvent
                }

            };
        }
    }
}