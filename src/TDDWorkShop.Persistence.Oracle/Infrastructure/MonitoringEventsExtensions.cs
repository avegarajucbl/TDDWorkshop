using System;

using Coolblue.Utilities.MonitoringEvents;
using Oracle.ManagedDataAccess.Client;

namespace TDDWorkShop.Persistence.Oracle.Infrastructure
{
    internal static class MonitoringEventsExtensions
    {
        //eg.
        //activity=save_person
        //activityContext(activity)=test_domain.save_person.oracle <- metric
        //failReason(activityContext(activity))=test_domain.save_person.oracle.failure_reason <- tag
        private static Func<string, string> activityContext = s => $"{s}.oracle";
        private static Func<string, string> failReason = s => $"{s}.failure_reason";
        private static Func<OracleException, string> errorCode = errCode => $"{errCode.ErrorCode}";

        public static void TestingOracleConnectivity(this MonitoringEvents monitoringEvents)
        {
            monitoringEvents.Metrics.IncrementCounter(activityContext("health_check"), Tags.Success);
        }

        public static void TestingOracleConnectivity(this MonitoringEvents monitoringEvents, OracleException ex)
        {
            var activity = "health_check";
            monitoringEvents.Logger.Error(ex, "Oracle exception testing Oracle connectivity");
            var tags = Tags.Failure.WithTag(failReason(activityContext(activity)), errorCode(ex));
            monitoringEvents.Metrics.IncrementCounter(activity, tags);
        }

        public static void TestingOracleConnectivity(this MonitoringEvents monitoringEvents, Exception ex)
        {
            var activity = "health_check";
            monitoringEvents.Logger.Error(ex, "Unexpected error testing Oracle connectivity");
            var tags = Tags.Failure.WithTag(failReason(activityContext(activity)), ex.HResult);
            monitoringEvents.Metrics.IncrementCounter(activity, tags);
        }

        public static void QueryExecuted(this MonitoringEvents monitoringEvents, string activity)
        {
            monitoringEvents.Metrics.IncrementCounter(activityContext(activity), Tags.Success);
        }

        public static void QueryExecuted(this MonitoringEvents monitoringEvents, string activity, OracleException ex)
        {
            monitoringEvents.Logger.Error(ex, $"Error occured while executing {activity}.");
            var tags = Tags.Failure.WithTag(failReason(activityContext(activity)), errorCode(ex));
            monitoringEvents.Metrics.IncrementCounter(activityContext(activity), tags);
        }
    }
}