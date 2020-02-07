using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Dauber.Azure.ApplicationInsights
{
    public class ApplicationInsightsTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public ApplicationInsightsTelemetryProcessor(ITelemetryProcessor next)
        {
            _next = next;
        }

        public void Process(Microsoft.ApplicationInsights.Channel.ITelemetry item)
        {
            if (item is DependencyTelemetry dependencyTelemetry)
            {
                if (dependencyTelemetry.Name == "AcceptMessageSession") return;
                if (dependencyTelemetry.Type == "AcceptMessageSession") return;
                if (dependencyTelemetry.Properties.ContainsKey("Outgoing Command") && dependencyTelemetry.Properties["Outgoing Command"] == "AcceptMessageSession") return;
                if (dependencyTelemetry.Type == "Azure Service Bus") return;
                // only log table storage writes over 1 second in duration
                if (dependencyTelemetry.Type == "Azure table" && dependencyTelemetry.Duration.TotalSeconds < 1) return;
            }

            if (item is ExceptionTelemetry exceptionTelemetry)
            {
                // these happen every 60 seconds on AcceptMessageSession and should be ignored
                if(exceptionTelemetry.ExceptionDetailsInfoList.Count > 0 &&
                   exceptionTelemetry.ExceptionDetailsInfoList[0].TypeName == "Microsoft.Azure.ServiceBus.ServiceBusTimeoutException") return;
            }

            _next.Process(item);
        }
    }
}
