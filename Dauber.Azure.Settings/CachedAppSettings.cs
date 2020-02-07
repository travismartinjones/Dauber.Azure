using System.Collections.Concurrent;
using System.Text;
using Dauber.Core.Contracts;
using Dauber.Core.Time;

namespace Dauber.Azure.Settings
{
    public class TimedCacheObject
    {
        public object Value { get; set; }
        public System.DateTime Expiration { get; set; }
    }
    
    public class TimedCacheString
    {
        public string Value { get; set; }
        public System.DateTime Expiration { get; set; }
    }

    public class CachedAppSettings : IAppSettings
    {
        private readonly IAppSettings _appSettings;
        private readonly IStorageSettings _storageSettings;
        private readonly IDateTime _dateTime;
        private readonly ConcurrentDictionary<string, TimedCacheObject> _storageObjectCache = new ConcurrentDictionary<string, TimedCacheObject>();
        private readonly ConcurrentDictionary<string, TimedCacheObject> _configObjectCache = new ConcurrentDictionary<string, TimedCacheObject>();
        private readonly ConcurrentDictionary<string, TimedCacheString> _storageStringCache = new ConcurrentDictionary<string, TimedCacheString>();
        private readonly ConcurrentDictionary<string, TimedCacheString> _configStringCache = new ConcurrentDictionary<string, TimedCacheString>();
        
        public CachedAppSettings(IAppSettings appSettings, IStorageSettings storageSettings, IDateTime dateTime)
        {
            _appSettings = appSettings;
            _storageSettings = storageSettings;
            _dateTime = dateTime;
        }

        public T GetByKey<T>(string key, T defaultValue)
        {
            _storageObjectCache.TryGetValue(key, out var existingValue);
            if (existingValue?.Expiration < _dateTime.UtcNow)
            {
                _storageObjectCache.TryRemove(key, out _);
            }
            else if (existingValue?.Value != null)
            {
                return (T) existingValue.Value;
            }

            _configObjectCache.TryGetValue(key, out existingValue);
            if (existingValue?.Expiration < _dateTime.UtcNow)
            {
                _configObjectCache.TryRemove(key, out _);
            }
            else if(existingValue?.Value != null)
            { 
                return (T)existingValue.Value;
            }

            var stringStorageValue = _storageSettings.GetByKey(key);
            if (!string.IsNullOrEmpty(stringStorageValue))
            {
                var storageValue = (T)_storageSettings.GetByKey<T>(key, defaultValue);
                var val = new TimedCacheObject
                {
                    Expiration = _dateTime.UtcNow.AddSeconds(60),
                    Value = storageValue
                };
                _storageObjectCache.AddOrUpdate(key, (s => val), (s, o) => val);
                return storageValue;
            }

            var appSettingValue = _appSettings.GetByKey(key, defaultValue);
            var appVal = new TimedCacheObject
            {
                Expiration = _dateTime.UtcNow.AddSeconds(60),
                Value = appSettingValue
            };
            _configObjectCache.AddOrUpdate(key, (s => appVal), (s, o) => appVal);
            return appSettingValue;
        }

        public string GetByKey(string key)
        {
            _storageStringCache.TryGetValue(key, out var existingValue);
            if (existingValue?.Expiration < _dateTime.UtcNow)
            {
                _storageStringCache.TryRemove(key, out _);
            }
            else if (existingValue?.Value != null)
            {
                return existingValue.Value;
            }

            _configStringCache.TryGetValue(key, out existingValue);
            if (existingValue?.Expiration < _dateTime.UtcNow)
            {
                _configStringCache.TryRemove(key, out _);
            }
            else if(existingValue?.Value != null)
            { 
                return existingValue.Value;
            }

            var storageValue = _storageSettings.GetByKey(key, "");
            if (!string.IsNullOrEmpty(storageValue))
            {
                var val = new TimedCacheString
                {
                    Expiration = _dateTime.UtcNow.AddSeconds(60),
                    Value = storageValue
                };
                _storageStringCache.AddOrUpdate(key, (s => val), (s, o) => val);
                return storageValue;
            }

            var appSettingValue = _appSettings.GetByKey(key, "");
            var appVal = new TimedCacheString
            {
                Expiration = _dateTime.UtcNow.AddSeconds(60),
                Value = appSettingValue
            };
            _configStringCache.AddOrUpdate(key, (s => appVal), (s, o) => appVal);
            return appSettingValue;
        }
    }
}