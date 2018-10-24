namespace TDDWorkShop
{
    public interface IRequestRegistery
    {
        bool RequestExists(RequestId requestId);
        void StoreRequest(RequestId requestId);
    }
}
