using NUnit.Framework;
using Shouldly;

namespace Dauber.Azure.DocumentDb.Test
{
    [TestFixture]
    public class HelpersSpecs
    {
        [Test]
        public void TestHelper()
        {
            var result = Helpers.GetPropertySelectNames<TestViewModel>("b");
            result.ShouldNotBeEmpty();
            result.ShouldContain("b.id");
            result.ShouldContain("b.Test");
            result.ShouldContain("b._etag");
            result.ShouldNotContain("b.ETag");
            result.ShouldStartWith("b");
        }
    }
}