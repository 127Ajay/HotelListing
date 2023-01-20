using HotelListing.API.Exceptions;
using Newtonsoft.Json;
using System.Net;

namespace HotelListing.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ExceptionMiddleware> logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Something went wrong while processing {context.Request.Path}.");
                await HandleExpectionAsync(context, ex);
            }
        }

        private Task HandleExpectionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType= "application/json";
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            var errorDetails = new ErrorDetails
            {
                ErrorType = "Failure",
                ErrorMessage = ex.Message,
            };

            switch(ex)
            {
                case NotFoundException notFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    errorDetails.ErrorType = "Not Found";
                    break;
                default: 
                    break;
            }

            string reponse = JsonConvert.SerializeObject(errorDetails);
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(reponse);
        }
    }

    public class ErrorDetails
    {
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
    }
}
