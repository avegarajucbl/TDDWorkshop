using System;

namespace TDDWorkShop
{
    public interface IUnitOfWork : IDisposable
    {
        void Start();
        void Complete();
    }
}
