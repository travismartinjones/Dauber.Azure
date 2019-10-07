using System;
using System.Text;
using System.Threading.Tasks;
using Dauber.Azure.Blob;
using Dauber.Azure.Blob.Contracts;
using Dauber.Core.Exceptions;
using Newtonsoft.Json;

namespace Dauber.Azure.Settings
{
    public class StorageSettings : IStorageSettings
    {
        private readonly IExceptionLogger _exceptionLogger;
        private readonly IBlobStore _blobStore;
        private const int RefreshIntervalSeconds = 60;
        private object _lock = new object();
        private dynamic _jArray;
        private const string EnvironmentKey = "Environment";
        
        public StorageSettings(
            string environment,
            IExceptionLogger exceptionLogger)
        {
            _exceptionLogger = exceptionLogger;
            _blobStore = new BlobStore(new SettingsBlobStoreSettings());
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var blob = await _blobStore.GetAsync($"{environment.ToLower()}/common.json", true).ConfigureAwait(false);
                        lock (_lock)
                        {
                            _jArray = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(blob.Data));
                        }
                        await Task.Delay(RefreshIntervalSeconds * 1000).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _exceptionLogger.Log(ex);
                    }
                }
            });
        }

        public T GetByKey<T>(string key, T defaultValue)
        {
            if (key == EnvironmentKey) return defaultValue;
            if (_jArray == null) return defaultValue;
            var value = _jArray.GetType().GetProperty(key)?.GetValue(_jArray, null);
            return value is T valueTyped ? valueTyped : defaultValue;
        }

        public string GetByKey(string key)
        {
            if (key == EnvironmentKey) return null;
            if (_jArray == null) return null;
            return _jArray.GetType().GetProperty(key)?.GetValue(_jArray, null)?.ToString();
        }
    }
}