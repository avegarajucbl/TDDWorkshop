using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.TestHost;
using Moq;
using TDDWorkShop;
using TDDWorkShop.Api;

namespace TDDWorkshop.Api.Tests.Integration.Controllers
{
    public class TestBase
    {
        protected TestServer _server;
        private Startup _startup;

        protected Mock<IProductsMeasurementUseCase> ProductMeasurementUseCaseMock
        = new Mock<IProductsMeasurementUseCase>();

        public TestBase()
        {
            _startup = new StartupTestable(new HostingEnvironment());

            _startup.RegisterOverrides = container =>
                                         {
                                             container.Options.AllowOverridingRegistrations = true;
                                             container.Register<IProductsMeasurementUseCase>(() => ProductMeasurementUseCaseMock.Object);
                                         };
            
            var webHostBuilder = new WebHostBuilder()
                                 //.UseStartup(typeof(StartupTestable))
                                 .ConfigureServices(collection => _startup.ConfigureServices(collection))
                                 .Configure(builder => _startup.Configure(builder, Mock.Of<IApiVersionDescriptionProvider>()))
                                 .UseEnvironment("Production");

            _server = new TestServer(webHostBuilder);
        }
    }
}
