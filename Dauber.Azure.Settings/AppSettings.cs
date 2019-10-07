using System.ComponentModel;
using Microsoft.Azure;

namespace Dauber.Azure.Settings
{
    public class AppSettings : IConfigurationManagerAppSettings
    {
        public T GetByKey<T>(string key, T defaultValue = default(T))
        {
            var appSetting = CloudConfigurationManager.GetSetting(key, false);

            if (string.IsNullOrWhiteSpace(appSetting))
            {
                return defaultValue;
            }

            var converter = TypeDescriptor.GetConverter(typeof(T));
            var appSettingValue = (T)(converter.ConvertFromInvariantString(appSetting));
            return appSettingValue;
        }

        public string GetByKey(string key)
        {
            var appSettingValue = CloudConfigurationManager.GetSetting(key, false);
            return appSettingValue;
        }
    }
}
