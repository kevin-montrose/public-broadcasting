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
    public class Readme
    {
        #pragma warning disable 0649
        class A
        {
            public string Prop { get; set; }
            public int Field;

            public byte NotInB { get; set; }
        }

        struct B
        {
            public string Prop;				// Not actually a property!
            public int Field { get; set; }	// Nor is this a field!
        }
        #pragma warning restore 0649

        [TestMethod]
        public void One()
        {
            var aBytes = Serializer.Serialize(new A { Prop = "Value", Field = 123, NotInB = 5 });
            var b = Serializer.Deserialize<B>(aBytes);

            Assert.IsTrue(b.Prop == "Value" && b.Field == 123);
        }

        class MyDictImpl<T, V> : Dictionary<T, V> { }
        class MyListImpl<T> : List<T> { }

        [TestMethod]
        public void Two()
        {
            var dBytes = Serializer.Serialize(new Dictionary<int, string> { { 123, "Hello" } });
            var iDict = Serializer.Deserialize<IDictionary<int, string>>(dBytes);
            var myDict = Serializer.Deserialize<MyDictImpl<int, string>>(dBytes);

            var lBytes = Serializer.Serialize(new List<string> { "Hello", "World" });
            var iList = Serializer.Deserialize<IList<string>>(lBytes);
            var myList = Serializer.Deserialize<MyListImpl<string>>(lBytes);
        }

        [TestMethod]
        public void Three()
        {
            var val = Serializer.Deserialize<int>(Serializer.Serialize((int?)null));

            Assert.AreEqual(0, val);
        }

        [TestMethod]
        public void Four()
        {
            dynamic dyn = Serializer.Deserialize(Serializer.Serialize(new { B = "C" }));

            var equiv = dyn.B == dyn["B"];

            Assert.IsTrue(equiv);
        }
    }
}
