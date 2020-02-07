using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dauber.Azure.DocumentDb
{
    public class CompositeTelemetryLogger : ITelemetryLogger
    {
        private ITelemetryLogger[] _loggers = null;

        public void ComposeOf(params ITelemetryLogger[] loggers)
        {
            _loggers = loggers;
        }

        public async Task Log(string type, double requestCharge, double duration, string methodName, string file, int lineNumber, string context)
        {
            if (_loggers == null) return;
            foreach (var logger in _loggers)
                await logger.Log(type, requestCharge, duration, methodName, file, lineNumber, context).ConfigureAwait(false);
        }
    }
}