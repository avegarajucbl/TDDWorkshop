using System;
using Microsoft.AspNetCore.Mvc;
using TDDWorkShop.Api.Requests;

namespace TDDWorkShop.Api.Controllers
{
    public class ProductsController
    {
        private readonly IProductsMeasurementUseCase _productsMeasurementUse;

        public ProductsController(IProductsMeasurementUseCase productsMeasurementUse)
        {
            _productsMeasurementUse = productsMeasurementUse;
        }

        public IActionResult Create(MeasurementsApiRequest measurementsRequest)
        {
            try
            {
                _productsMeasurementUse.Execute(measurementsRequest.ToProductMeasurement());
                return new OkResult();
            }
            catch(Exception e)
            {
                return new NotFoundResult();
            }
        }
    }
}
