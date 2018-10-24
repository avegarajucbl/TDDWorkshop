using System;
using System.Net;
using System.Threading.Tasks;

using Coolblue.Utilities.MonitoringEvents;

using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;

namespace TDDWorkShop.Api.Infrastructure
{
    public class ExceptionHandlingMiddleware
    {
        public async Task Invoke(HttpContext context, Func<Task> next)
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                _monitoringEvents.UnhandledException(ex);
                if (_respondOnError)
                {
                    await WriteErrorResponseAsync(context, ex);
                }
                else
                {
                    await WriteErrorCodeAsync(context);
                }
            }
        }

        private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = ex.Message,
            };
            var errorMessageJson = JsonConvert.SerializeObject(response);

            await context.Response.WriteAsync(errorMessageJson);
        }

        private static async Task WriteErrorCodeAsync(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var response = new
            {
                error = "Oh no! Something went wrong.",
            };
            var errorMessageJson = JsonConvert.SerializeObject(response);
            await context.Response.WriteAsync(errorMessageJson);
        }

        public ExceptionHandlingMiddleware(MonitoringEvents monitoringEvents, bool respondOnError = false)
        {
            _monitoringEvents = monitoringEvents;
            _respondOnError = respondOnError;
        }

        private readonly MonitoringEvents _monitoringEvents;
        internal readonly bool _respondOnError;
    }
}