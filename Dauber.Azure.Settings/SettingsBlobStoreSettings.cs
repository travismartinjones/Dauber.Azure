using Dauber.Azure.Blob.Contracts;

namespace Dauber.Azure.Settings
{
    public class SettingsBlobStoreSettings : IBlobSettings
    {
        public string ConnectionString => "DefaultEndpointsProtocol=https;AccountName=daubersettings;AccountKey=mf7R/qbVAz9s37DCjDcTdQlRCodiZJMzmoDT1LlbjpEeR/X/mRzfh3Y+SS3Sag2HJ/kLMWyEs4JPylr/+EEKeQ==;EndpointSuffix=core.windows.net";
    }
}