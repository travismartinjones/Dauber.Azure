using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dauber.Azure.Settings
{
    public class JsonPropertyService
    {
        private JObject _jObject;

        public void SetJson(string json)
        {
            _jObject = JObject.Parse(json);
        }

        public string GetValue(string key)
        {
            return _jObject?[key]?.ToString();
        }
    }
}