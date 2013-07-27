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

            var bytes = Serializer.Serialize(original);

            var obj = Serializer.Deserialize<FourTestObject>(bytes);

            Assert.AreEqual(original.AProperty, obj.AProperty);
            Assert.AreEqual(original.Values, obj.Values);
            Assert.AreEqual(original.Values.Count, obj.Values.Count);
            Assert.AreEqual(original.Values["something"], obj.Values["something"]);
        }

        [TestMethod]
        public void Five()
        {
            var original = new Dictionary<string, object>
                {
                    { "fooString", "bar" },
                    { "fooObj", new TwoTestObject { Property1 = "bar" } }
                };

            var bytes = Serializer.Serialize(original);

            var obj = Serializer.Deserialize<Dictionary<string, object>>(bytes);

            Assert.AreEqual(original.Values, obj.Values);
            Assert.AreEqual(original.Values.Count, obj.Values.Count);
            Assert.AreEqual(original["fooString"], obj["fooString"]);
            Assert.AreEqual(((TwoTestObject)original["fooObj"]).Property1, ((TwoTestObject)obj["fooObj"]).Property1);
        }
    }
}
