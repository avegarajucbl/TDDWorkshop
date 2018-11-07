using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TDDWorkShop.Api.Controllers;
using TDDWorkShop.Api.Requests;
using Xunit;

namespace TDDWorkshop.Api.Tests.Unit.Controllers
{
    public class ProductsControllerTests
    {
        [Fact]
        public void Create_WithAValidInput_ReturnsOk()
        {
            var measurementsRequest = new MeasurementsApiRequest();
            

            var sut = new ProductsController();

            var result = sut.Create(measurementsRequest);

            result.Should().BeEquivalentTo(new OkResult());
        }

    }
}
