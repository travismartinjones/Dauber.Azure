using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Dauber.Azure.Blob;
using Dauber.Azure.Blob.Contracts;
using Dauber.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Dauber.Azure.Settings
{
    public class StorageSettings : IStorageSettings
    {
        private const int RefreshIntervalSeconds = 60;
        private readonly JsonPropertyService _jsonPropertyService = new JsonPropertyService();
        
        public StorageSettings(
            string environment,
            ISettingsBlobStoreSettings settings,
            IExceptionLogger exceptionLogger)
        {
            var blobStore = new BlobStore(settings);
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var blob = await blobStore.GetAsync($"{environment.ToLower()}/common.json", true).ConfigureAwait(false);

                        _jsonPropertyService.SetJson(Encoding.UTF8.GetString(blob.Data));
                    }
                    catch (Exception ex)
                    {
                        exceptionLogger.Log(ex);
                    }
                    finally
                    {
                        await Task.Delay(RefreshIntervalSeconds * 1000).ConfigureAwait(false);
                    }
                }
            });
        }


        public T GetByKey<T>(string key, T defaultValue)
        {
            var value = _jsonPropertyService.GetValue(key);
            if(_jsonPropertyService == null)
                throw new Exception("JsonPropertyService is null");
            if (value == null) return defaultValue;
            return (T)(TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value));
        }

        public string GetByKey(string key)
        {
            var value = _jsonPropertyService.GetValue(key);
            if(_jsonPropertyService == null)
                throw new Exception("JsonPropertyService is null");
            if (value == null) return "";
            return (string)(TypeDescriptor.GetConverter(typeof(string)).ConvertFromInvariantString(value));
        }
    }
}