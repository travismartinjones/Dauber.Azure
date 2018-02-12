using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dauber.Core.Contracts;
using Shouldly;
using Xunit;

namespace Dauber.Azure.DocumentDb.Test
{
    public class TestViewModel : ViewModel
    {
        public bool Test { get; set; }
    }

    public class HelpersSpecs
    {
        [Fact]
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
