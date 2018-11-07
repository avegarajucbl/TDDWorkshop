using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using TDDWorkShop.Api.Requests;
using TDDWorkShop.Exceptions;

namespace TDDWorkShop.Api.Controllers
{
    public class ProductsController: Controller
    {
        private readonly IProductsMeasurementUseCase _productsMeasurementUse;

        public ProductsController(IProductsMeasurementUseCase productsMeasurementUse)
        {
            _productsMeasurementUse = productsMeasurementUse;
        }

        [HttpPost("api/createMeasurements")]
        public IActionResult Create(MeasurementsApiRequest measurementsRequest)
        {
            try
            {
                _productsMeasurementUse.Execute(measurementsRequest.ToProductMeasurement());
                return new OkResult();
            }
            catch(ProductNotFoundException productNotFoundException)
            {
                return new NotFoundResult();
            }
            catch(Exception e)
            {
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
