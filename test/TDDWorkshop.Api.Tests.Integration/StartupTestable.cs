using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;
using TDDWorkShop;
using TDDWorkShop.Api;

namespace TDDWorkshop.Api.Tests.Integration
{
    public class StartupTestable: Startup
    {
        Mock<IProductsMeasurementUseCase> _usecaseMock = new Mock<IProductsMeasurementUseCase>();
        public StartupTestable(IHostingEnvironment env)
                : base(env)
        {
            RegisterOverrides = container =>
                                {
                                    container.Options.AllowOverridingRegistrations = true;
                                    container.Register<IProductsMeasurementUseCase>(() => _usecaseMock.Object);
                                };
        }

        protected override IConfigurationRoot GetConfiguration()
        {
            var setting = new Dictionary<string, string>()
                          {
                                  {
                                          "OracleConnectionString", "Data source = TEST"
                                  },
                                  {
                                          "OracleResilience:RetryCount", "3"
                                  },
                                  {
                                          "OracleResilience:RetryDelayMilliseconds", "100"
                                  },
                                  {
                                          "OracleResilience:CircuitBreakerAllowedAttempts", "5"
                                  },
                                  {
                                          "OracleResilience:CircuitBreakerDurationMilliseconds", "5000"
                                  },
                          };

            return new ConfigurationBuilder()
                   .AddInMemoryCollection(setting)
                   .Build();
        }
    }
}
