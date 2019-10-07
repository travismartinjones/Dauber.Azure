using System;
using System.Collections.Generic;
using Dauber.Core.Collections;
using Dauber.Core.Contracts;
using Dauber.Core.Time;

namespace Dauber.Azure.Settings
{
    public class CachedAppSettings : IAppSettings
    {
        private readonly AppSettings _appSettings;
        private readonly IStorageSettings _storageSettings;
        private readonly ConcurrentTimeoutDictionary<string, object> _concurrentTimeoutDictionaryTyped;
        private readonly ConcurrentTimeoutDictionary<string, string> _concurrentTimeoutDictionaryString;

        public CachedAppSettings(AppSettings appSettings, IStorageSettings storageSettings, IDateTime dateTime)
        {
            _concurrentTimeoutDictionaryTyped = new ConcurrentTimeoutDictionary<string, object>(dateTime, new TimeSpan(0, 0, 60));
            _concurrentTimeoutDictionaryString = new ConcurrentTimeoutDictionary<string, string>(dateTime, new TimeSpan(0, 0, 60));
            _appSettings = appSettings;
            _storageSettings = storageSettings;
        }

        public T GetByKey<T>(string key, T defaultValue)
        {
            if (_concurrentTimeoutDictionaryTyped.TryGetValue(key, out var returnValue))
                return (T)returnValue;

            var storageValue = _storageSettings.GetByKey<T>(key, defaultValue);
            if (!EqualityComparer<T>.Default.Equals(storageValue, defaultValue))
            {
                _concurrentTimeoutDictionaryTyped[key] = storageValue;
                return storageValue;
            }

            var appSettingValue = _appSettings.GetByKey(key, defaultValue);
            _concurrentTimeoutDictionaryTyped[key] = appSettingValue;
            return appSettingValue;
        }

        public string GetByKey(string key)
        {
            if (_concurrentTimeoutDictionaryString.TryGetValue(key, out var returnValue))
                return returnValue;

            var storageValue = _storageSettings.GetByKey(key);
            if (!string.IsNullOrEmpty(storageValue))
            {
                _concurrentTimeoutDictionaryString[key] = storageValue;
                return storageValue;
            }

            var appSettingValue = _appSettings.GetByKey(key);
            _concurrentTimeoutDictionaryString[key] = appSettingValue;
            return appSettingValue;
        }
    }
}