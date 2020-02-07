using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Dauber.Azure.ApplicationInsights;
using Dauber.Azure.DocumentDb;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Dauber.Azure.DocumentDB.ApplicationInsights
{
    public class TelemetryLogger : ITelemetryLogger
    {
        public async Task Log(string type, double requestCharge, double duration, string methodName, string file, int lineNumber, string context)
        {
            var telemetryClient = new TelemetryClient();

            var dependency = new DependencyTelemetry(
                "Azure DocumentDB",
                "documents.azure.com",
                type,
                "",
                DateTimeOffset.UtcNow,
                new TimeSpan(0,0,0,0,(int)duration),
                "0", // Result code : we can't capture 429 here anyway
                true // We assume this call is successful, otherwise an exception would be thrown before.
            );
            dependency.Metrics[TelemetryKeys.RequestCharge] = requestCharge;
            dependency.Properties[TelemetryKeys.Method] = methodName;
            dependency.Properties[TelemetryKeys.Filename] = file;
            dependency.Properties[TelemetryKeys.Line] = lineNumber.ToString();
            dependency.Properties[TelemetryKeys.Context] = context.ToString();
            telemetryClient.TrackDependency(dependency);
        }
    }
}
