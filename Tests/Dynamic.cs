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
    }
}
