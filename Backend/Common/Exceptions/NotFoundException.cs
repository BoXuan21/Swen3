namespace Swen3.API.Common.Exceptions
{
    /// <summary>
    /// Exception for resource not found errors
    /// Maps to HTTP 404 Not Found
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
        
        public NotFoundException(string resourceType, object id) 
            : base($"{resourceType} with id {id} was not found.")
        {
        }
    }
}

