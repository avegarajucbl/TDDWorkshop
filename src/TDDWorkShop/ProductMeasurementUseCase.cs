using System;

namespace TDDWorkShop
{
    public class ProductMeasurementUseCase: IProductsMeasurementUseCase
    {
        private readonly IProductMeasurementDataStore _productMeasurementDataStore;
        private readonly IRequestRegistery _requestRegistery;
        private readonly IUnitOfWork _unitOfWork;

        public ProductMeasurementUseCase(IProductMeasurementDataStore productMeasurementDataStore,
                                         IRequestRegistery requestRegistery,
                                         IUnitOfWork unitOfWork)
        {
            _productMeasurementDataStore = productMeasurementDataStore;
            _requestRegistery = requestRegistery;
            _unitOfWork = unitOfWork;
        }
        public void Execute(ProductsMeasurement productsMeasurement)
        {
            using(_unitOfWork)
            {
                _unitOfWork.Start();
                _requestRegistery.StoreRequest(Guid.NewGuid());

                _productMeasurementDataStore.CreateMeasurement(productsMeasurement);
                _unitOfWork.Complete();
            }
            
        }
    }
}
