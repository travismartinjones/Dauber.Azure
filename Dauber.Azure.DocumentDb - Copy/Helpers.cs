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
            var properties = typeof(T).GetProperties().ToList();
            var propertyNames = properties
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? p.Name)
                .ToList();            
            return $"{prefix}.{string.Join($", {prefix}.", propertyNames)}";
        }
    }
}