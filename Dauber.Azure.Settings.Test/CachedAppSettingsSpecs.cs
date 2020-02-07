using Dauber.Core.Contracts;
using Dauber.Core.Exceptions;
using Dauber.Core.Time;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace Dauber.Azure.Settings.Test
{
    public class CachedAppSettingsSpecs
    {
        [Test]
        public void when_cached_settings_do_not_exist_but_app_settings_do()
        {
            var appSettings = A.Fake<IAppSettings>();
            var storageSettings = A.Fake<IStorageSettings>();
            var sut = new CachedAppSettings(appSettings, storageSettings, new DateTime());
            A.CallTo(() => appSettings.GetByKey("IsProcessingSamsaraLocation", false)).Returns(true);
            A.CallTo(() => storageSettings.GetByKey("IsProcessingSamsaraLocation")).Returns(null);
            A.CallTo(() => storageSettings.GetByKey("IsProcessingSamsaraLocation", false)).Returns(false);
            sut.GetByKey("IsProcessingSamsaraLocation",false).ShouldBe(true);
        }
    }

    public class StorageSettingsSpecs
    {
        [Test]
        public void test()
        {
            var blobStorageSettings = A.Fake<ISettingsBlobStoreSettings>();
            var sut = new StorageSettings("", blobStorageSettings, A.Fake<IExceptionLogger>());
            sut.GetByKey("IsProcessingSamsaraLocation").ShouldBeNull();
            sut.GetByKey("IsProcessingSamsaraLocation",false).ShouldBeFalse();
        }
    }
}