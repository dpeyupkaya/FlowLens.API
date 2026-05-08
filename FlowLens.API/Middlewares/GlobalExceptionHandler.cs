using FlowLens.API.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FlowLens.API.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Global Exception Caught: {Message}", exception.Message);

            var statusCode = exception switch
            {
                FluentValidation.ValidationException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                InvalidOperationException => (int)HttpStatusCode.Conflict,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var errorResponse = new ErrorDetails
            {
                StatusCode = statusCode,
                Error = GetErrorTitle(statusCode),

                Message = exception is FluentValidation.ValidationException valEx
                    ? string.Join(" | ", valEx.Errors.Select(e => e.ErrorMessage))
                    : exception.Message,

                Details = _env.IsDevelopment() ? exception.StackTrace : null
            };

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsync(errorResponse.ToString(), cancellationToken);

            return true;
        }

        private static string GetErrorTitle(int statusCode)
        {
            return statusCode switch
            {
                400 => "Geçersiz İstek",
                401 => "Yetkisiz Erişim",
                403 => "Erişim Reddedildi",
                404 => "Kaynak Bulunamadı",
                409 => "İşlem Çakışması",
                _ => "Sunucu Hatası"
            };
        }
    }
}