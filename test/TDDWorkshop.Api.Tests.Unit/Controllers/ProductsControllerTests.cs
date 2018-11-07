using System;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TDDWorkShop;
using TDDWorkShop.Api.Controllers;
using TDDWorkShop.Api.Requests;
using TDDWorkShop.Exceptions;
using Xunit;

namespace TDDWorkshop.Api.Tests.Unit.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductsMeasurementUseCase> _productsMeasurementUseCaseMock = new Mock<IProductsMeasurementUseCase>();

        [Fact]
        public void Create_WithAValidInput_ReturnsOk()
        {
            var measurementsRequest = new MeasurementsApiRequest();
            IActionResult result = ExecuteSut(measurementsRequest);

            result.Should().BeEquivalentTo(new OkResult());
        }

        [Fact]
        public void Create_WithInvalidProduct_ReturnsNotFound()
        {
            var measurementsRequest = new MeasurementsApiRequest();

            _productsMeasurementUseCaseMock.Setup(obj => obj.Execute(It.IsAny<ProductsMeasurement>()))
                                           .Throws<ProductNotFoundException>();

            var sut = new ProductsController(_productsMeasurementUseCaseMock.Object);

            var result = sut.Create(measurementsRequest);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Create_WhenUseCaseThrowsUnknownException_ReturnsInternalServerError()
        {
            var measurementsRequest = new MeasurementsApiRequest();

            _productsMeasurementUseCaseMock.Setup(obj => obj.Execute(It.IsAny<ProductsMeasurement>()))
                                           .Throws<Exception>();

            var sut = new ProductsController(_productsMeasurementUseCaseMock.Object);

            var result = sut.Create(measurementsRequest);

            result.Should().BeOfType<StatusCodeResult>().Which.StatusCode
                  .Should().Be((int) HttpStatusCode.InternalServerError);
        }

        private IActionResult ExecuteSut(MeasurementsApiRequest measurementsRequest)
        {
            var sut = new ProductsController(_productsMeasurementUseCaseMock.Object);

            var result = sut.Create(measurementsRequest);
            return result;
        }
    }
}
