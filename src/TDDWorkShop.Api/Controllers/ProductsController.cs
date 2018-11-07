using Microsoft.AspNetCore.Mvc;
using TDDWorkShop.Api.Requests;

namespace TDDWorkShop.Api.Controllers
{
    public class ProductsController
    {
        public IActionResult Create(MeasurementsApiRequest measurementsRequest)
        {
            return new OkResult();
        }
    }
}
