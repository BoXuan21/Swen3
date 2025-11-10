using System.Net;
using System.Text.Json;
using Swen3.API.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Swen3.API.Middleware
{
    /// <summary>
    /// Global exception handling middleware that catches all exceptions,
    /// logs them, and returns appropriate HTTP responses
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = exception switch
            {
                ValidationException validationEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = validationEx.Message,
                    Errors = validationEx.Errors
                },
                
                NotFoundException => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = exception.Message
                },
                
                DomainException => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = exception.Message
                },
                
                RepositoryException => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "A database error occurred. Please try again later."
                },
                
                MessagingException => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                    Message = "A messaging service error occurred. Please try again later."
                },
                
                _ => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "An unexpected error occurred. Please try again later."
                }
            };

            // Log at appropriate level based on exception type
            switch (exception)
            {
                case ValidationException:
                    _logger.LogWarning(exception, "Validation error: {Message}. RequestPath: {RequestPath}", 
                        exception.Message, context.Request.Path);
                    break;
                case NotFoundException:
                    _logger.LogWarning("Resource not found: {Message}. RequestPath: {RequestPath}", 
                        exception.Message, context.Request.Path);
                    break;
                case DomainException:
                    _logger.LogWarning(exception, "Domain error: {Message}. RequestPath: {RequestPath}", 
                        exception.Message, context.Request.Path);
                    break;
                case RepositoryException:
                    _logger.LogError(exception, "Repository error. RequestPath: {RequestPath}, Method: {Method}", 
                        context.Request.Path, context.Request.Method);
                    break;
                case MessagingException:
                    _logger.LogError(exception, "Messaging error. RequestPath: {RequestPath}, Method: {Method}", 
                        context.Request.Path, context.Request.Method);
                    break;
                default:
                    _logger.LogError(exception, "Unexpected error. RequestPath: {RequestPath}, Method: {Method}", 
                        context.Request.Path, context.Request.Method);
                    break;
            }

            context.Response.StatusCode = response.StatusCode;
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }

        private class ErrorResponse
        {
            public int StatusCode { get; set; }
            public string Message { get; set; } = string.Empty;
            public Dictionary<string, string[]>? Errors { get; set; }
        }
    }
}

