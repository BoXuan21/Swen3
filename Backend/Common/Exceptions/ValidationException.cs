namespace Swen3.API.Common.Exceptions
{
    /// <summary>
    /// Exception for validation errors (invalid input, business rules)
    /// Maps to HTTP 400 Bad Request
    /// </summary>
    public class ValidationException : DomainException
    {
        public Dictionary<string, string[]>? Errors { get; }

        public ValidationException(string message) : base(message) { }
        
        public ValidationException(string message, Dictionary<string, string[]> errors) 
            : base(message)
        {
            Errors = errors;
        }
        
        public ValidationException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}

