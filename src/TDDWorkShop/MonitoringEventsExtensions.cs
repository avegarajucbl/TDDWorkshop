using System;
using Coolblue.Utilities.MonitoringEvents;

namespace TDDWorkShop
{
    public static class MonitoringEventsExtensions
    {
        private static Func<string, string> failReason = s => $"{s}.failure_reason";
        public static void SkippedCreatePersonAsAdult(this MonitoringEvents monitoringEvents, string activity)
        {
            var tags = Tags.Failure
                .WithTag(failReason(activity), "dependency_unavailable")
                .WithTag("as", "adult");
            monitoringEvents.Metrics.IncrementCounter(activity, tags);
        }

        public static void CreatedAdult(this MonitoringEvents monitoringEvents, string activity)
        {
            var tags = Tags.Success
                .WithTag("as", "adult");
            monitoringEvents.Metrics.IncrementCounter(activity, tags);
        }

        public static void UpdatedPerson(this MonitoringEvents monitoringEvents, string activity)
        {
            var tags = Tags.Success;
            monitoringEvents.Metrics.IncrementCounter(activity, tags);
        }

        public static void FailedToCreateAdult(this MonitoringEvents monitoringEvents, string activity, Exception ex)
        {
            monitoringEvents.Logger.Error(activity, ex);
            var tags = Tags.Failure
                .WithTag("as", "adult");
            monitoringEvents.Metrics.IncrementCounter(activity, tags);
        }

        public static void FailedToUpdatePerson(this MonitoringEvents monitoringEvents, string activity, Exception ex)
        {
            monitoringEvents.Logger.Error(activity, ex);
            var tags = Tags.Failure;
            monitoringEvents.Metrics.IncrementCounter(activity, tags);
        }

        public static void FailedToQueryAllPeople(this MonitoringEvents monitoringEvents, string activity, Exception ex)
        {
            monitoringEvents.Logger.Error(activity, ex);
            var tags = Tags.Failure;
            monitoringEvents.Metrics.IncrementCounter(activity, tags);
        }

        public static void VersionMismatch(this MonitoringEvents monitoringEvents, string activity, Exception ex)
        {
            monitoringEvents.Logger.Error(activity, ex);
            var tags = Tags.Failure;
            monitoringEvents.Metrics.IncrementCounter(activity, tags);
        }

        public static void AbortBecausePersonExists(this MonitoringEvents monitoringEvents, string activity, Exception ex)
        {
            monitoringEvents.Logger.Error(activity, ex);
            var tags = Tags.Failure;
            monitoringEvents.Metrics.IncrementCounter(activity, tags);
        }
    }
}

