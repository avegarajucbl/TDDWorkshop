using TDDWorkShop.Persistence.Oracle;
using Microsoft.Extensions.Configuration;

using Serilog.Events;
using System.Net;

namespace TDDWorkShop.Api
{
    public class WebServiceSettings
    {
        public string SplunkHost { get; set; } = IPAddress.Loopback.ToString();
        public int SplunkUdpPort { get; set; } = 514;
        public string SeqUrl { get; set; }
        public LogEventLevel SplunkMinimumLogLevel { get; set; } = LogEventLevel.Information;
        public LogEventLevel GlobalMinimumLogLevel { get; set; } = LogEventLevel.Information;
        public string OracleConnectionString { get; set; }

        public PersistenceAdapterSettings PersistenceAdapterSettings { get; private set; }

        public WebServiceSettings(IConfiguration configuration)
        {
            configuration.Bind(this);
            PersistenceAdapterSettings = new PersistenceAdapterSettings(configuration);
        }
    }
}