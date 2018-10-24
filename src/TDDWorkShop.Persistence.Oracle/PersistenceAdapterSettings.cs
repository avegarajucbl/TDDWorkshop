using Coolblue.Utilities.Resilience;
using Microsoft.Extensions.Configuration;

namespace TDDWorkShop.Persistence.Oracle
{
    public class PersistenceAdapterSettings
    {
        public string OracleConnectionString { get; set; }
        public ResilienceSettings OracleResilience { get; set; }

        public PersistenceAdapterSettings(IConfiguration configuration)
        {
            configuration.Bind(this);
        }
    }
}