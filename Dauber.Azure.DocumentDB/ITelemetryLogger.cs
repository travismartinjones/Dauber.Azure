using System.Globalization;
using System.Threading.Tasks;

namespace Dauber.Azure.DocumentDb
{
    public interface ITelemetryLogger
    {
        Task Log(string type, double requestCharge, double duration, string methodName, string file, int lineNumber, string context);
    }
}