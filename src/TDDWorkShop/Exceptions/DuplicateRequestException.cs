using System;

namespace TDDWorkShop
{
    public class DuplicateRequestException : Exception
    {
        public DuplicateRequestException(RequestId id) : base($"The RequestId {id} has already been submitted.")
        { }
        public DuplicateRequestException(RequestId id, Exception innerException) : base($"The RequestId {id} has already exists.", innerException)
        { }

        public DuplicateRequestException(string message) : base(message)
        { }

        public DuplicateRequestException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
