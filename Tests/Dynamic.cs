﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PublicBroadcasting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class Dynamic
    {
        [TestMethod]
        public void Dictionaries()
        {
            var bytes = Serializer.Serialize(new Dictionary<string, int>() { { "Hello", 1 }, { "World", 2 } });
            dynamic dynDict = Deserializer.Deserialize(bytes);

            Assert.AreEqual(2, dynDict.Count);
            Assert.AreEqual(1, dynDict["Hello"]);
            Assert.AreEqual(2, dynDict["World"]);
        }

        [TestMethod]
        public void Anon()
        {
            var bytes = Serializer.Serialize(new { A = "Foo", B = "Bar", C = "Bazz", D = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } });
            dynamic dynAnon = Deserializer.Deserialize(bytes);

            Assert.AreEqual("Foo", dynAnon.A);
            Assert.AreEqual("Bar", dynAnon.B);
            Assert.AreEqual("Bazz", dynAnon.C);
            Assert.AreEqual(10, dynAnon.D.Count);
            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(i + 1, dynAnon.D[i]);
            }
        }

        [TestMethod]
        public void Indexer()
        {
            var bytes = Serializer.Serialize(new { A = "Foo", B = 123, C = (int?)2, C2 = (int?)null, D = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } });
            var dyn = Deserializer.Deserialize(bytes);

            Assert.AreEqual("Foo", dyn["A"]);
            Assert.AreEqual(123, dyn["B"]);
            Assert.AreEqual(2, dyn["C"]);
            Assert.AreEqual(null, dyn["C2"]);
            Assert.AreEqual(10, dyn["D"].Count);

            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(i + 1, dyn["D"][i]);
            }
        }
    }
}
