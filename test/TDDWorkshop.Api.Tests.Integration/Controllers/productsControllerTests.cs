using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using TDDWorkShop.Api;
using TDDWorkShop.Api.Requests;
using Xunit;

namespace TDDWorkshop.Api.Tests.Integration.Controllers
{
    public class ProductsControllerTests
    {
        private TestServer _server;

        public ProductsControllerTests()
        {
            var webHostBuilder = new WebHostBuilder()
                                 .UseStartup(typeof (StartupTestable))
                                 .UseEnvironment("Production");
            
            _server = new TestServer(webHostBuilder);
        }


        [Fact]
        public void Create_WithAValidInput_ReturnsOk()
        {
            var response = CreateRequest();

            response.Should().BeOfType<HttpResponseMessage>()
                    .Which.StatusCode.Should().Be((int) HttpStatusCode.OK);
        }

        private HttpResponseMessage CreateRequest()
        {
            return _server.CreateRequest("api/createMeasurements").PostAsync()
                   .GetAwaiter().GetResult();
        }
    }
}
