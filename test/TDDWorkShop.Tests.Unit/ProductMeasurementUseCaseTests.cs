using System.Transactions;
using FluentAssertions;
using Moq;
using Xunit;

namespace TDDWorkShop.Tests.Unit
{
    public class ProductMeasurementUseCaseTests
    {
        private Mock<IProductMeasurementDataStore> _productMeasurementDataStore = new Mock<IProductMeasurementDataStore>();
        private Mock<IRequestRegistery> _requestRegistry = new Mock<IRequestRegistery>();
        private IUnitOfWork _untiOfWork = new UnitOfWorkTransactionScope();

        [Fact]
        public void Execute_RequestAndMeasurements_ShouldBePersistedInTheSameTransaction()
        {
            var sut = new ProductMeasurementUseCase(_productMeasurementDataStore.Object,
                    _requestRegistry.Object,
                    _untiOfWork);

            Transaction actualTransactionProductMeasurement = null;
            Transaction actualTransactionRequestRegistery = null;

            _productMeasurementDataStore.Setup(obj => obj.CreateMeasurement(It.IsAny<ProductsMeasurement>()))
                                        .Callback((ProductsMeasurement _) =>
                                                  {
                                                      actualTransactionProductMeasurement = Transaction.Current;
                                                  });

            _requestRegistry.Setup(obj => obj.StoreRequest(It.IsAny<RequestId>()))
                            .Callback((RequestId _) => { actualTransactionRequestRegistery = Transaction.Current; });

            sut.Execute(new ProductsMeasurement());

            actualTransactionRequestRegistery.Should().NotBeNull();
            actualTransactionRequestRegistery.Should().BeSameAs(actualTransactionProductMeasurement);
        }

    }
}
