using System.ComponentModel;
using NUnit.Framework;
using Shouldly;

namespace Dauber.Azure.Settings.Test
{
    public class JsonPropertyServiceSpecs
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void extracting_a_property_by_id_extracts_the_expected_value()
        {
            var sut = new JsonPropertyService();
            sut.SetJson(
@"
{
  ""IsEventHubEnabled"": false
}
");
            sut.GetValue("IsEventHubEnabled").ToLower().ShouldBe("false");
        }


        [Test]
        public void a_missing_property_extracts_the_expected_value()
        {
            var sut = new JsonPropertyService();
            sut.SetJson(
@"
{
  ""IsEventHubEnabled"": false
}
");
            sut.GetValue("IsProcessingSamsaraLocation").ShouldBeNull();
        }
    }
}