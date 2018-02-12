using System.Linq;
using System.Reflection;
using Dauber.Core.Contracts;
using Newtonsoft.Json;

namespace Dauber.Azure.DocumentDb
{
    public static class Helpers
    {
        public static string GetPropertySelectNames<T>(string prefix = "c") where T : ViewModel
        {
            return $"{prefix}.{string.Join($", {prefix}.", typeof(T).GetProperties().Select(p => p.GetCustomAttribute<JsonPropertyAttribute>()).Select(jp => jp.PropertyName))}";
        }
    }
}