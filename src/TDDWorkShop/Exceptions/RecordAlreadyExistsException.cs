using System;

namespace TDDWorkShop
{
    public class RecordAlreadyExistsException : Exception
    {
        public RecordAlreadyExistsException() : base("The record already exists in the data storage. Did you mean to perform an update instead?")
        { }

        public RecordAlreadyExistsException(object obj) : base($"The record {obj} already exists in the data storage. Did you mean to perform an update instead?")
        { }

        public RecordAlreadyExistsException(string message) : base(message)
        { }

        public RecordAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
