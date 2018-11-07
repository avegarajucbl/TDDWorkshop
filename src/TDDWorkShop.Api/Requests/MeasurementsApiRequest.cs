using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TDDWorkShop.Api.Requests
{
    public class MeasurementsApiRequest
    {
        public ulong ProductId { get; set; }
        public string IdentificationCode { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public int MaxStackQuantity { get; set; }
        public int MaxClampQuantity { get; set; }

        public ProductsMeasurement ToProductMeasurement()
        {
            return new ProductsMeasurement
                   {
                           Width = Width,
                           IdentificationCode = IdentificationCode,
                           Length = Length,
                           ProductId = ProductId,
                           MaxClampQuantity = MaxClampQuantity,
                           MaxStackQuantity = MaxStackQuantity,
                           Height = Height
                   };
        }
    }
}
