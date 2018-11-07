using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Moq;
using TDDWorkShop;
using TDDWorkShop.Api;
using TDDWorkShop.Api.Requests;
using Xunit;

namespace TDDWorkshop.Api.Tests.Integration.Controllers
{
    public class ProductsControllerTests : TestBase
    {
        

        public ProductsControllerTests()
        {

            
        }

        [Fact]
        public void Create_WithAValidInput_ReturnsOk()
        {
            var response = CreateRequest();

            ProductMeasurementUseCaseMock.Setup(obj => obj.Execute(It.IsAny<ProductsMeasurement>()));
                                         
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
