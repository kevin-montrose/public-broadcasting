using Microsoft.VisualStudio.TestTools.UnitTesting;
using PublicBroadcasting;
using PublicBroadcasting.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class POCOBuilder
    {
        class Foo
        {
            public string Bar { get; set; }
            public int Buzz { get; set; }

            internal double IgnoredProp { get; set; }

            public int IgnoredField = 314;

            public void MyFavoriteMethod() { }
        }

        [TestMethod]
        public void Class()
        {
            var map = PublicBroadcasting.Impl.POCOBuilder<Foo, AllPublicDescriber<Foo>>.GetMapper();

            dynamic poco = map.GetMapper()(new Foo { Bar = "Hello", Buzz = 8675309 });

            Assert.AreEqual("Hello", poco.Bar);
            Assert.AreEqual(8675309, poco.Buzz);
        }
    }
}
