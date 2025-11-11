namespace Swen3.API.Common.Exceptions
{
    /// <summary>
    /// Base exception for domain/business logic errors
    /// Maps to HTTP 400 Bad Request
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
        
        public DomainException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}

