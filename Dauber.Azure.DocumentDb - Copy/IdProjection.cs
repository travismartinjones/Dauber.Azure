using System;
using Newtonsoft.Json;

namespace Dauber.Azure.DocumentDb
{
    public class IdProjection
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
    }
}