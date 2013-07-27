using System.Web.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PublicBroadcasting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class Issues
    {
        private class TwoTestObject
        {
            public string Property1 { get; set; }
            public string ThisBreaksThings
            {
                get
                {
                    return string.Empty;
                }
            }
        }

        [TestMethod]
        public void Two()
        {
            var bytes = Serializer.Serialize(new TwoTestObject { Property1 = "Foo" });

            var obj = Serializer.Deserialize<TwoTestObject>(bytes);

            Assert.AreEqual("Foo", obj.Property1);
            Assert.AreEqual(string.Empty, obj.ThisBreaksThings);
        }

        private class ThreeTestObject
        {
            // The order of these makes a difference.
            public TwoTestObject BProperty { get; set; }
            public TwoTestObject AProperty { get; set; }
        }

        [TestMethod]
        public void Three()
        {
            var bytes = Serializer.Serialize(new ThreeTestObject());

            var obj = Serializer.Deserialize<ThreeTestObject>(bytes);
        }

        private class FourTestObject
        {
            public string AProperty { get; set; }
            public RouteValueDictionary Values { get; set; }
        }

        [TestMethod]
        public void Four()
        {
            var original = new FourTestObject
                {
                    AProperty = "foo",
                    Values = new RouteValueDictionary { { "something", "lies here" } }
                };

            try
            {
                var bytes = Serializer.Serialize(original);
                Assert.Fail("Serializing *should* fail");
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual("Type Tests.Issues+FourTestObject contains a System.Object, which cannot be sensibly serialized.  Use a more specific type.  (path to System.Object reference: Root.Values.<ValueType>)", e.Message);
            }
        }
    }
}
