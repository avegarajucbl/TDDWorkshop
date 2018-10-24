using System;
using System.Net;
using System.Threading.Tasks;
using Coolblue.Utilities.MonitoringEvents;
using Coolblue.Utilities.CorrelationContext;
using PhilosophicalMonkey;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace TDDWorkShop.Api.Infrastructure
{
    internal static class MonitoringEventsExtensions
    {
        public static void ApplicationStart(this MonitoringEvents monitoringEvents)
        {
            monitoringEvents.Logger.Information("Starting Domain Web API, version {Version}",
                Reflect.OnTypes.GetAssembly(typeof(MonitoringEventsExtensions)).GetName().Version);
        }

        public static void UnhandledException(this MonitoringEvents monitoringEvents, Exception exception)
        {
            monitoringEvents.Logger.Error(exception, "Unhandled exception in application.");
        }

        public static void HttpRequestStarted(this MonitoringEvents monitoringEvents, HttpContext context)
        {
            monitoringEvents.LogContext.PushProperties(
                new LogContextProperty("CorrelationId", CorrelationContext.CorrelationId),
                new LogContextProperty("IncomingAttemptId", CorrelationContext.AttemptId),
                new LogContextProperty("HttpMethod", context.Request.Method),
                new LogContextProperty("HttpUri", context.Request.Path + context.Request.QueryString),
                new LogContextProperty("HttpRemoteHost", context.Connection.RemoteIpAddress));

            monitoringEvents.Logger.Information("HTTP request: {HttpMethod} {HttpUri} from {HttpRemoteHost}",
                                                context.Request.Method,
                                                context.Request.Path + context.Request.QueryString,
                                                context.Connection.RemoteIpAddress);
        }

        public static void HttpRequestCompleted(this MonitoringEvents monitoringEvents,
                                                HttpContext context,
                                                TimeSpan elapsedTime)
        {
            monitoringEvents.Logger.Information("HTTP request complete: {HttpStatusCode} {HttpMimeType} after " +
                                                "{RequestElapsedTime} ms",
                                                context.Response.StatusCode,
                                                context.Response.ContentType,
                                                elapsedTime.TotalMilliseconds);
            monitoringEvents.Metrics.Timer("httprequest.elapsed",
                                           elapsedTime,
                                           "method:" + context.Request.Method,
                                           "status:" + context.Response.StatusCode);
        }

        public static void UnexpectedExceptionReturnedByUseCase(this MonitoringEvents monitoringEvents,
                                                                Exception ex)
        {
            monitoringEvents.Logger.Warning(ex, "Unexpected exception returned by use case.");
        }
    }
}