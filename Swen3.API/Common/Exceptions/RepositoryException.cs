namespace Swen3.API.Common.Exceptions
{
    /// <summary>
    /// Exception for data access layer errors (database operations, etc.)
    /// Maps to HTTP 500 Internal Server Error
    /// </summary>
    public class RepositoryException : Exception
    {
        public RepositoryException(string message) : base(message) { }
        
        public RepositoryException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}

