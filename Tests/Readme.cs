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

        class C
        {
            public int Mem1;
            public long Mem2;
        }

        class D
        {
            public int Mem1;
            public int Mem2;
        }

        class E
        {
            public long Mem1;
            public long Mem2;
        }

        [TestMethod]
        public void Five()
        {
            var cBytes = Serializer.Serialize(new C { Mem1 = 123, Mem2 = 456 });
            var cAsLongDict = Serializer.Deserialize<Dictionary<string, long>>(cBytes);
            var cAsDoubleDict = Serializer.Deserialize<Dictionary<string, double>>(cBytes);

            Assert.AreEqual(2, cAsLongDict.Count);
            Assert.AreEqual(2, cAsDoubleDict.Count);

            Assert.AreEqual(123, cAsLongDict["Mem1"]);
            Assert.AreEqual(123, cAsDoubleDict["Mem1"]);

            Assert.AreEqual(456, cAsLongDict["Mem2"]);
            Assert.AreEqual(456, cAsDoubleDict["Mem2"]);

            var nada = Serializer.Deserialize<Dictionary<string, byte>>(cBytes);
            Assert.AreEqual(0, nada.Count);

            var dictBytes = Serializer.Serialize(new Dictionary<string, int> { { "Mem1", 3141 }, { "Mem2", 1318 } });
            var d = Serializer.Deserialize<D>(dictBytes);
            var e = Serializer.Deserialize<E>(dictBytes);

            Assert.AreEqual(3141, d.Mem1);
            Assert.AreEqual(3141, e.Mem1);

            Assert.AreEqual(1318, d.Mem2);
            Assert.AreEqual(1318, e.Mem2);
        }
    }
}
