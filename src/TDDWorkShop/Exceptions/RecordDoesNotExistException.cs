using System;

namespace TDDWorkShop
{
    public class RecordDoesNotExistException : Exception
    {
        public RecordDoesNotExistException() : base("The record does not exist in the data storage. Did you mean to perform an insert instead?")
        { }

        public RecordDoesNotExistException(object obj) : base($"The record {obj} does not exist in the data storage. Did you mean to perform an insert instead?")
        { }

        public RecordDoesNotExistException(string message) : base(message)
        { }

        public RecordDoesNotExistException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
